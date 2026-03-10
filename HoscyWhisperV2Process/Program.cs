// #define DBG_AUDIO

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
        /*
        Desired flow of operations:
        1. [ ] Reading of configuration variables from args
        2. [ ] Setting up IPC and phoning home
        3. [ ] Initialization of all required systems
        4. [ ] Starting of main flow using CT
        5. [ ] Listening to mute and stop events
        6. [ ] Shutdown on CT
        */


        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();

        using var audioEngine = CreateAudioEngine(logger);
        using var capture = CreateCaptureDevice(audioEngine, string.Empty, logger); //todo: config

        var bytesPerSecond = 16_000 * 2;
        var bytesPer10ms = bytesPerSecond / 100;

        using var vad = CreateVad(logger);
        var audioProcessor = new AudioProcessor(vad, new());

        var path = Console.ReadLine()!;
        using var processor = CreateProcessor(path, logger);

        var recCore = new WhisperRecognitionCore(processor, audioProcessor, capture, logger);

        using var cts = new CancellationTokenSource();
        var task = Task.Run(async () => await recCore.RecognizeAsync(cts.Token));

        await recCore.AwaitStartedAsync(cts.Token); //todo: fix?
        capture.SetListening(true);
        Console.ReadLine();
        capture.SetListening(false);
        cts.Cancel();
        await task;
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
            //.WithSegmentEventHandler(_handler)
            //.WithNoContext()
            .Build();
    }
}