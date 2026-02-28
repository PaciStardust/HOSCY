using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using Serilog;
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
public class TestRecognitionModule(ILogger logger, IAudioService audio)
    : RecognitionModuleBase(logger.ForContext<TestRecognitionModule>())
{
    #region Vars
    private readonly IAudioService _audio = audio;

    private AudioCaptureDeviceProxy? _mic = null;
    #endregion

    #region Infos / Events
    public override bool IsListening => _mic?.IsListening ?? false;
    #endregion

    #region Start / Stop

    protected override void StartForService()
    {
        _mic = _audio.CreateCaptureDeviceProxy();
        _mic.OnAudioProcessed += OnAudioProcessed;
        _mic.Start();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override void StopForRecognitionModule()
    {
        _mic?.Stop();
        _mic?.OnAudioProcessed -= OnAudioProcessed;
        _mic?.Dispose();
        _mic = null;
    }

    protected override bool IsStarted()
    {
        return _mic is not null && _mic.IsStarted;
    }

    protected override bool IsProcessing()
    {
        return IsStarted() && IsListening;
    }
    #endregion

    #region Control
    protected override bool SetListeningForRecognitionModule(bool state)
    {
        _mic?.SetListening(state);
        return IsListening;
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion

    #region Handling
    private void OnAudioProcessed(Span<byte> samples, Capability _)
    {
        var sum = 0f;
        foreach(var sample in samples)
        {
            sum += sample;
        }
        var avg = sum / Math.Min(samples.Length, 1);
        _logger.Verbose("Processed: Count={count} Sum={sum} Avg={avg}",
            samples.Length.ToString().PadRight(16),
            sum.ToString().PadRight(16),
            avg.ToString().PadRight(16));
    }
    #endregion
}