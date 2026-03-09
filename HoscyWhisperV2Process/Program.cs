// #define DBG_AUDIO

using System.Buffers.Binary;
using System.Diagnostics;
using HoscyCore.Services.Audio;
using Serilog;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;
using WebRtcVadSharp;
using Whisper.net;

namespace HoscyWhisperV2Process;

public class Program
{   
    public static async Task Main(string[] args)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .CreateLogger();

        using var audioEngine = CreateAudioEngine(logger);
        using var capture = CreateCaptureDevice(audioEngine, string.Empty, logger); //todo: config

        var bytesPerSecond = 16_000 * 2;
        var bytesPer10ms = bytesPerSecond / 100;

        using var vad = CreateVad(logger);
        var audioProcessor = new AudioProcessor(vad, new());

        var path = Console.ReadLine()!;
        using var processor = CreateProcessor(path, logger);

        using var ms = new MemoryStream();
        ResetStream(ms);

        capture.Start();
        capture.SetListening(true);
        var sw = Stopwatch.StartNew();
        capture.OnAudioProcessed += (audioData, _) =>
        {
            var segments =  audioData.Length / bytesPer10ms;
            var bools = new bool[segments];

            for (var i = 0; i < segments; i++)
            {
                var slice = audioData.Slice(i * bytesPer10ms, bytesPer10ms);
                var result = audioProcessor.Process10msFrame(slice);

                if (result == FrameProcessingResult.Empty)
                {
                    #if DBG_AUDIO
                        Console.Write(" ");
                    #endif
                    continue;
                }

                ms.Write(slice);

                switch (result)
                {
                    case FrameProcessingResult.ContinueAndProcess:
                        #if DBG_AUDIO 
                            Console.Write("!");
                        #endif
                        HandleRecognition(ms, processor);
                        continue;

                    case FrameProcessingResult.Continue:
                        #if DBG_AUDIO 
                            Console.Write(".");
                        #endif
                        continue;

                    case FrameProcessingResult.CancelAndProcess:
                        #if DBG_AUDIO 
                            Console.Write("&");
                        #endif
                        HandleRecognition(ms, processor);
                        goto case FrameProcessingResult.Cancel;

                    default:
                    case FrameProcessingResult.Cancel:
                        #if DBG_AUDIO 
                            Console.Write("_");
                        #endif
                        ResetStream(ms);
                        continue;
                }
            }
        };

        Console.ReadLine();
    }

    private static void HandleRecognition(MemoryStream ms, WhisperProcessor processor)
    {
        #if !DBG_AUDIO

        var pos = ms.Position;
        ms.Position = 0;
        var newBuffer = new MemoryStream();
        ms.CopyTo(newBuffer, (int)pos);

        WriteRestOfHeader(newBuffer.GetBuffer().AsSpan(), pos);
        Task.Run(() => {
            try
            {
                newBuffer.Position = 0;
                processor.Process(newBuffer);
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}: {ex.StackTrace}");
            }
            newBuffer.Dispose();
        });
        ms.Position = pos;
        #endif
    }

    private static void ResetStream(MemoryStream ms)
    {
        ms.SetLength(0);
        ms.Write(_baseHeader);
    }

    private static AudioEngine CreateAudioEngine(ILogger logger)
    {
        logger.Debug("Starting audio engine");
        var engine = new MiniAudioEngine();
        engine.UpdateAudioDevicesInfo();
        return engine;
    }

    private static AudioCaptureDeviceProxy CreateCaptureDevice(AudioEngine engine, string devName, ILogger logger)
    {
        logger.Debug("Creating audio device");

        var devInfo = AudioUtils.FindDevice(engine.CaptureDevices, devName, logger) 
            ?? throw new ArgumentException("Failed to locate a suitable microphone");

        var format = new AudioFormat()
        {
            Channels = 1,
            Format = SampleFormat.S16,
            Layout = ChannelLayout.Mono,
            SampleRate = 16_000
        };

        var rawDevice = engine.InitializeCaptureDevice(devInfo, format);
        return new(rawDevice, logger);
    }

    private static WebRtcVad CreateVad(ILogger logger)
    {
        logger.Debug("Creating VAD");
        return new WebRtcVad()
        {
            OperatingMode = OperatingMode.Aggressive, //todo: config
            SampleRate = SampleRate.Is16kHz,
            FrameLength = FrameLength.Is10ms
        };
    } 

    private static readonly OnSegmentEventHandler _handler = (e) =>
    {
        Console.WriteLine(e.Text);
    };

    private static WhisperProcessor CreateProcessor(string path, ILogger logger) //todo: config
    {
        logger.Debug("Creating processor");
        var whisperFactory = WhisperFactory.FromPath(path);
        return whisperFactory.CreateBuilder()
            .WithLanguage("en")
            //.WithLanguageDetection()
            .WithPrintResults()
            .WithPrintSpecialTokens()
            .WithPrintTimestamps()
            .WithSegmentEventHandler(_handler)
            //.WithNoContext()
            .Build();
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

    private static void WriteRestOfHeader(Span<byte> dataWithHeader, long len)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(dataWithHeader.Slice(4, 4), (uint)len - 8);
        BinaryPrimitives.WriteUInt32LittleEndian(dataWithHeader.Slice(40, 4), (uint)len - 44);
    }
}