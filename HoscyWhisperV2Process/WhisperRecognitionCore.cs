// #define DBG_AUDIO

using System.Buffers.Binary;
using HoscyCore.Services.Audio;
using Serilog;
using SoundFlow.Enums;
using Whisper.net;

namespace HoscyWhisperV2Process;

public class WhisperRecognitionCore(WhisperProcessor whisperProcessor, AudioProcessor audioProcessor, AudioCaptureDeviceProxy audioCapture, ILogger logger) //todo: config
{
    private readonly WhisperProcessor _whisperProcessor = whisperProcessor;
    private readonly AudioProcessor _audioProcessor = audioProcessor;
    private readonly AudioCaptureDeviceProxy _audioCapture = audioCapture;
    private readonly ILogger _logger = logger;

    private bool _isRunning = false;
    public async Task RecognizeAsync(CancellationToken ct)
    {
        _isRunning = false;
        _logger.Debug("Starting recognition");

        Task recTask = Task.Run(() => RunRecognitionAsync(ct));

        _audioCapture.OnAudioProcessed += ProcessAudioFrames;
        _audioCapture.Start();

        _isRunning = true;
        await recTask;

        //todo: exception handling

        _audioCapture.Stop();
        _audioCapture.OnAudioProcessed -= ProcessAudioFrames;

        _isRunning = false;
        _logger.Debug("Stopped recognition");
    }

    public async Task AwaitStartedAsync(CancellationToken ct)
    {
        while(!_isRunning)
        {
            if (ct.IsCancellationRequested) return;
            await Task.Delay(5);
        }
        return;
    }

    private const int BYTES_PER_SECOND = 16_000 * 2;
    private const int BYTES_PER_10_MS = BYTES_PER_SECOND / 100;
    private void ProcessAudioFrames(Span<byte> audioFrames, Capability _) //todo: does this need a lock
    {
        var frameCount = audioFrames.Length / BYTES_PER_10_MS;

        for (var i = 0; i < frameCount; i++)
        {
            var frame = audioFrames.Slice(i * BYTES_PER_10_MS, BYTES_PER_10_MS);
            ProcessSingleAudioFrame(frame);
        }
    }

    private readonly MemoryStream _audioStream = new(); //todo: dispose
    private uint _currentSegmentId = 0;
    private void ProcessSingleAudioFrame(Span<byte> audioFrame)
    {
        var result = _audioProcessor.Process10msFrame(audioFrame);

        if (result == FrameProcessingResult.Empty)
        {
            #if DBG_AUDIO
                Console.Write(" ");
            #endif
            return;
        }

        _audioStream.Write(audioFrame);

        switch (result)
        {
            case FrameProcessingResult.ContinueAndProcess:
                #if DBG_AUDIO 
                    Console.Write("!");
                #endif
                SetNewRecognitionData();
                return;

            case FrameProcessingResult.Continue:
                #if DBG_AUDIO 
                    Console.Write(".");
                #endif
                return;

            case FrameProcessingResult.CancelAndProcess:
                #if DBG_AUDIO 
                    Console.Write("&");
                #endif
                SetNewRecognitionData();
                goto case FrameProcessingResult.Cancel;

            default:
            case FrameProcessingResult.Cancel:
                #if DBG_AUDIO 
                    Console.Write("_");
                #endif
                _audioStream.SetLength(0);
                _currentSegmentId++; //todo: better logging everywhere?
                return;
        }
    }

    private void SetNewRecognitionData()
    {
        if (_nextUpProcessing.HasValue)
        {
            var nextUpId = _nextUpProcessing.Value.Id;
            if (nextUpId < _currentSegmentId)
            {
                _logger.Error("Queue not empty, new entry has ID {newId}, which is SOMEHOW lower than current ID {currentId} => No override",
                    _currentSegmentId, nextUpId);
                return;
            } 
            else if (nextUpId == _currentSegmentId)
            {
                _logger.Debug("Queue not empty, new entry has ID {newId}, which is the same as current ID {currentId} => Overriding data as it is fresher",
                    _currentSegmentId, nextUpId);
            }
            else
            {
                _logger.Warning("Queue not empty, new entry has ID {newId}, which is higher than current ID {currentId} => Processing likely unable to keep up and model should maybe be swapped with a smaller one",
                    _currentSegmentId, nextUpId);
                //todo: killing this after a few succeeding occurances
            }
        }

        try
        {
            var streamPos = Convert.ToInt32(_audioStream.Position);
            var recognitionData = new byte[streamPos + 44]; //Wave header

            Buffer.BlockCopy(_audioStream.GetBuffer(), 0, recognitionData, 44, streamPos);
            Buffer.BlockCopy(_baseHeader, 0, recognitionData, 0, _baseHeader.Length);
            WriteRestOfHeader(recognitionData.AsSpan());


            _nextUpProcessing = (_currentSegmentId, recognitionData);
            _currentCts.Cancel();
        } catch (Exception ex)
        {
            _logger.Error(ex, "Creation of recognition data failed");
            return;
        }
    }

    private static readonly byte[] _baseHeader = CreateBaseWavHeader();
    private static byte[] CreateBaseWavHeader()
    {
        byte[] header = [
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            0, 0, 0, 0, // File size tbd
            (byte)'W', (byte)'A', (byte)'V', (byte)'E',
            (byte)'f', (byte)'m', (byte)'t', (byte)' ',
            16, 0, 0, 0, // Format data len
            1, 0, 1, 0, // PCM / Channel
            0, 0, 0, 0, // Sample rate tdb
            0, 0, 0, 0, // Byte rate tbd
            2, 0, 16, 0, // Block size, Bits per sample
            (byte)'d', (byte)'a', (byte)'t', (byte)'a',
            0, 0, 0, 0 // Data size tbd
        ];

        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(24), 16_000); // Sample Rate
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(28), 32_000); // Byte Rate

        return header;
    }

    private static void WriteRestOfHeader(Span<byte> dataWithHeader)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(dataWithHeader.Slice(4, 4), (uint)dataWithHeader.Length - 8);
        BinaryPrimitives.WriteUInt32LittleEndian(dataWithHeader.Slice(40, 4), (uint)dataWithHeader.Length - 44);
    }

    private (uint Id, byte[] Data)? _nextUpProcessing = null;
    private readonly CancellationTokenSource _currentCts = new(); //todo: dispose
    private async Task RunRecognitionAsync(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            await RunRecognitionAsyncTick(ct);
        }        
    }

    private async Task RunRecognitionAsyncTick(CancellationToken ct)
    {
        if (!_nextUpProcessing.HasValue)
        {
            await Task.Delay(20);
            return;
        }

        var currentBytes = _nextUpProcessing.Value.Data.ToArray();
        var currentId = _nextUpProcessing.Value.Id;
        _nextUpProcessing = null;
        _currentCts.TryReset();

        using var ms = new MemoryStream(currentBytes);
        ms.Position = 0;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _currentCts.Token);

        await foreach (var segment in _whisperProcessor.ProcessAsync(ms, linkedCts.Token))
        {
            Console.WriteLine(segment.Text); //todo: handle
        }
        
        //todo: detect cancel?

        if (_nextUpProcessing is not null)
        {
            //todo: handle logging of cancel or instant upcoming
        }
    }
}

