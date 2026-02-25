using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;

namespace HoscyCore.Services.Recognition.Modules;

[PrototypeLoadIntoDiContainer(typeof(TestRecognitionModuleStartInfo), Lifetime.Singleton)]
public class TestRecognitionModuleStartInfo : IRecognitionModuleStartInfo
{
    public string Name => "Test Recognizer";
    public string Description => "For testing only";
    public Type ModuleType => typeof(TestRecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Microphone;
}

[PrototypeLoadIntoDiContainer(typeof(TestRecognitionModule), Lifetime.Transient)]
public class TestRecognitionModule(ILogger logger, IAudioService audio) : StartStopModuleBase, IRecognitionModule //todo: [FEAT] calling of events, translator version
{
    #region Vars
    private readonly ILogger _logger = logger.ForContext<TestRecognitionModule>();
    private readonly IAudioService _audio = audio;

    private AudioCaptureDevice? _mic = null;
    private bool _muted = true;
    #endregion

    #region Infos / Events
    public bool IsListening => !_muted;

    public event Action<string> OnSpeechRecognized = delegate { };
    public event Action<bool> OnSpeechActivity = delegate { };
    #endregion

    #region Start / Stop

    protected override void StartInternal()
    {
        _logger.Information("Starting test recognizer");
        _mic = _audio.CreateCaptureDevice();
        _mic.OnAudioProcessed += OnAudioProcessed;
        _mic.Start();
        _logger.Information("Started test recognizer");
    }

    protected override void StopInternal()
    {
        _logger.Information("Stopping test recognizer");
        _mic?.Stop();
        _mic?.OnAudioProcessed -= OnAudioProcessed;
        _mic?.Dispose();
        _mic = null;
        _logger.Information("Stopped test recognizer");
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }

    protected override bool IsStarted()
    {
        return _mic is not null && _mic.IsRunning;
    }

    protected override bool IsProcessing()
    {
        return IsStarted() && IsListening;
    }
    #endregion

    #region Control
    public bool SetListening(bool state)
    {
        _muted = !state;
        return state;
    }
    #endregion

    #region Handling
    private void OnAudioProcessed(Span<float> samples, Capability _)
    {
        var sum = 0f;
        foreach(var sample in samples)
        {
            sum += sample;
        }
        var avg = sum / samples.Length;
        _logger.Verbose("Processed: Count={count} Sum={sum} Avg={avg}",
            samples.Length.ToString().PadRight(16),
            sum.ToString().PadRight(16),
            avg.ToString().PadRight(16));
    }
    #endregion
}