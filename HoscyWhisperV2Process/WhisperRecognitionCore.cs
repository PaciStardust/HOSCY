// #define DBG_AUDIO

using System.Buffers.Binary;
using HoscyCore.Services.Audio;
using Serilog;
using SoundFlow.Enums;
using Whisper.net;

namespace HoscyWhisperV2Process;

public class WhisperRecognitionCore(WhisperProcessor whisperProcessor, AudioProcessor audioProcessor, AudioCaptureDeviceProxy audioCapture, ILogger logger)
    : IDisposable
{
    #region Inject
    private readonly WhisperProcessor _whisperProcessor = whisperProcessor;
    private readonly AudioProcessor _audioProcessor = audioProcessor;
    private readonly AudioCaptureDeviceProxy _audioCapture = audioCapture;
    private readonly ILogger _logger = logger;    
    #endregion

    #region Main Task
    public bool IsRunning { get; private set; } = false;
    public async Task RecognizeAsync(CancellationToken ct, Action<RecognitionCallbackArgs> callback)
    {
        IsRunning = false;
        _logger.Information("Starting recognition");

        Task recTask = Task.Run(() => RunRecognitionAsync(ct, callback));

        _audioCapture.OnAudioProcessed += ProcessAudioFrames;
        _audioCapture.Start();

        IsRunning = true;
        await recTask;

        if (recTask.Exception is not null)
        {
            _logger.Error(recTask.Exception, "Recognition stopping with Exception");
        }

        _audioCapture.Stop();
        _audioCapture.OnAudioProcessed -= ProcessAudioFrames;

        IsRunning = false;
        _logger.Information("Stopped recognition");
    }
    #endregion

    #region Audio Processing
    private const int BYTES_PER_SECOND = 16_000 * 2;
    private const int BYTES_PER_10_MS = BYTES_PER_SECOND / 100;
    private void ProcessAudioFrames(Span<byte> audioFrames, Capability _)
    {
        var frameCount = audioFrames.Length / BYTES_PER_10_MS;

        for (var i = 0; i < frameCount; i++)
        {
            var frame = audioFrames.Slice(i * BYTES_PER_10_MS, BYTES_PER_10_MS);
            ProcessSingleAudioFrame(frame);
        }
    }

    private readonly MemoryStream _audioStream = new();
    private uint _activelyRecordingSegmentId = 1;
    private uint _activelyRecordingSegmentSubId = 0;
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
                #else
                    SetNewRecognitionData();
                #endif
                return;

            case FrameProcessingResult.Continue:
                #if DBG_AUDIO 
                    Console.Write(".");
                #endif
                return;

            case FrameProcessingResult.CancelAndProcess:
                #if DBG_AUDIO 
                    Console.Write("&");
                #else
                    SetNewRecognitionData();
                #endif
                goto case FrameProcessingResult.Cancel;

            default:
            case FrameProcessingResult.Cancel:
                #if DBG_AUDIO 
                    Console.Write("_");
                #endif
                _audioStream.SetLength(0);
                _activelyRecordingSegmentId++;
                _activelyRecordingSegmentSubId = 0;
                return;
        }
    }

    private void SetNewRecognitionData()
    {
        _activelyRecordingSegmentSubId++;
        _logger.Verbose("Setting new recognition data (ID {id}-{subId})",
            _activelyRecordingSegmentId, _activelyRecordingSegmentSubId);

        if (_processingQueueItem.HasValue)
        {
            var processingQueueItemId = _processingQueueItem.Value.Id;
            if (_activelyRecordingSegmentId < processingQueueItemId)
            {
                _logger.Error("Queue not empty, new entry has ID {newId}, which is SOMEHOW lower than queue ID {queueId} => No override",
                    _activelyRecordingSegmentId, processingQueueItemId);
                return;
            } 
            else if (_activelyRecordingSegmentId == processingQueueItemId)
            {
                _logger.Debug("Queue not empty, new entry has ID {newId}, which is the same as queue ID {queueId} => Overriding data as it is fresher",
                    _activelyRecordingSegmentId, processingQueueItemId);
            }
            else
            {
                _logger.Warning("Queue not empty, new entry has ID {newId}, which is higher than queue ID {queueId} => Processing likely unable to keep up and model should maybe be swapped with a smaller one",
                    _activelyRecordingSegmentId, processingQueueItemId);
            }
        }

        try
        {
            var streamPos = Convert.ToInt32(_audioStream.Position);
            var recognitionData = new byte[streamPos + 44]; //Wave header

            Buffer.BlockCopy(_audioStream.GetBuffer(), 0, recognitionData, 44, streamPos);
            Buffer.BlockCopy(_baseHeader, 0, recognitionData, 0, _baseHeader.Length);
            WriteRestOfHeader(recognitionData.AsSpan());

            _processingQueueItem = (_activelyRecordingSegmentId, _activelyRecordingSegmentSubId, recognitionData);
            _currentCts.Cancel();
            _logger.Verbose("Set new recognition data with ID {id}-{subId} and length {len}",
                _activelyRecordingSegmentId, _activelyRecordingSegmentSubId, recognitionData.Length);
        } catch (Exception ex)
        {
            _logger.Error(ex, "Creation of recognition data failed (ID {id}-{subId})",
                _activelyRecordingSegmentId, _activelyRecordingSegmentSubId);
            return;
        }
    }
    #endregion

    #region WAV Utils
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
    #endregion

    #region Actual Recognition
    private (uint Id, uint SubId, byte[] Data)? _processingQueueItem = null;
    private CancellationTokenSource _currentCts = new();
    private async Task RunRecognitionAsync(CancellationToken ct, Action<RecognitionCallbackArgs> callback)
    {
        _logger.Debug("Entering recognition data handling loop");
        while(!ct.IsCancellationRequested)
        {
            await RunRecognitionAsyncTick(ct, callback);
        }        
        _logger.Debug("Leaving recognition data handling loop");
    }

    private async Task RunRecognitionAsyncTick(CancellationToken ct, Action<RecognitionCallbackArgs> callback)
    {
        if (!_processingQueueItem.HasValue)
        {
            await Task.Delay(20);
            return;
        }

        var currentBytes = _processingQueueItem.Value.Data.ToArray();
        var currentId = _processingQueueItem.Value.Id;
        var currentSubId = _processingQueueItem.Value.SubId;
        _logger.Verbose("Now handling recognition data with ID {id}-{subId} and length {len}",
            currentId, currentSubId, currentBytes.Length);

        _processingQueueItem = null;
        if (!_currentCts.TryReset())
        {
            _currentCts.Dispose();
            _currentCts = new();
        }

        using var ms = new MemoryStream(currentBytes);
        ms.Position = 0;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _currentCts.Token);
        try
        {
            _logger.Verbose("Sending data with ID {id}-{subId} to recognition",
                currentId,currentSubId);

            uint seg = 1;
            var strings = new List<string>();
    
            await foreach (var segment in _whisperProcessor.ProcessAsync(ms, linkedCts.Token))
            {
                callback(new()
                {
                    Id = currentId, SubId = currentSubId, SegId = seg++, Data = segment
                });
                strings.Add(segment.Text);
            }

            if (strings.Count > 0)
            {
                var fullResult = string.Join(" ", strings);
                _logger.Debug("Recognition ID {id}-{subId} result: {result}",
                    currentId, currentSubId, fullResult);

                callback(new()
                {
                    Id = currentId, SubId = currentSubId, SegId = 0,
                    Data = new(fullResult, TimeSpan.MinValue, TimeSpan.MaxValue, 1, 1, 1, 0, string.Empty, [])
                });
            }
            else
            {
                _logger.Debug("Recognition ID {id}-{subId} yielded no result", currentId, currentSubId);
            }
        }
        catch (TaskCanceledException)
        {
            if (_processingQueueItem.HasValue)
            {
                var newId = _processingQueueItem.Value.Id;
                if (newId == currentId)
                {
                    _logger.Debug("Cancelled current process for ID {id}-{subId}, new data with same ID", newId, currentSubId);
                }
                else if (newId > currentId)
                {
                    _logger.Warning("Cancelled current process for ID {id}-{subId}, new data with higher ID {newId} - This might be a sign of not being able to keep up, consider picking a weaker model",
                        currentId, currentSubId, newId);
                }
                else
                {
                    _logger.Warning("Cancelled current process for ID {id}-{subId}, new data with lower ID {newId} - This should never happen",
                        currentId, currentSubId, newId);
                }
            }
            else
            {
                if (ct.IsCancellationRequested)
                    _logger.Debug("Main CT was cancelled, stopping recognition (ID {id}-{subId})", 
                        currentId, currentSubId);
                else 
                    _logger.Warning("CT triggered but no new data found? (ID {id}-{subId})", 
                        currentId, currentSubId);
            }
        }
        catch (WhisperProcessingException ex)
        {
            _logger.Warning("Whisper Error (ID {id}-{subId}): {message}",
                currentId, currentSubId, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Recognition encountered an exception of type {exType} (ID {id}-{subId})",
                ex.GetType().Name, currentSubId, currentId);
        }        
    }
    #endregion

    #region Cleanup
    public void Dispose()
    {
        _audioStream.Dispose();
        _currentCts.Dispose();
    }
    #endregion
}

