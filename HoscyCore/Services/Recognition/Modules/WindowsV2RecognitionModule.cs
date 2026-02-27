using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Speech.Recognition;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Enums;

namespace HoscyCore.Services.Recognition.Modules;

[SupportedOSPlatform("windows")]
[PrototypeLoadIntoDiContainer(typeof(WindowsV2RecognitionModuleStartInfo), Lifetime.Singleton, SupportedPlatformFlags.Windows)]
public class WindowsV2RecognitionModuleStartInfo : IRecognitionModuleStartInfo
{
    public WindowsV2RecognitionModuleStartInfo()
    {
        OtherUtils.ThrowOnInvalidPlatform([OSPlatform.Windows]);
    }

    public string Name => "Windows Recognizer V2";
    public string Description => "Recognizer using Windows Recognition, low quality, please avoid";
    public Type ModuleType => typeof(WindowsV2RecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Microphone | RecognitionModuleConfigFlags.Windows;
}

[SupportedOSPlatform("windows")]
[PrototypeLoadIntoDiContainer(typeof(WindowsV2RecognitionModule), Lifetime.Transient, SupportedPlatformFlags.Windows)]
public class WindowsV2RecognitionModule : WindowsRecognitionModuleBase
{
    #region Vars
    public WindowsV2RecognitionModule
    (
        ILogger logger, 
        ConfigModel config, 
        IRecognitionModelProviderService modelProvider,
        IAudioService audio
    )
        : base(logger.ForContext<WindowsV2RecognitionModule>(), config, modelProvider)
    {
        OtherUtils.ThrowOnInvalidPlatform([OSPlatform.Windows]);

        _audio = audio;
    }

    private readonly IAudioService _audio;

    private SpeechRecognitionEngine? _engine = null;
    private SpeechStreamer? _stream = null;
    private AudioCaptureDeviceProxy? _mic = null;
    #endregion

    #region Start / Stop
    protected override void StartForService()
    {
        _stream = new(16000);

        var mic = _audio.CreateCaptureDevice();
        mic.OnAudioProcessed += HandleAudioProcessed;
        mic.Start();
        _mic = mic;

        var engine = CreateEngine();
        engine.LoadGrammar(new DictationGrammar());
        engine.SpeechDetected += HandleSpeechDetected;
        engine.SpeechRecognized += HandleSpeechRecognized;
        engine.SetInputToAudioStream(_stream, 
            new(16000, System.Speech.AudioFormat.AudioBitsPerSample.Sixteen, System.Speech.AudioFormat.AudioChannel.Mono));
        engine.RecognizeAsync(RecognizeMode.Multiple);
        _engine = engine;
    }

    private void HandleAudioProcessed(Span<byte> span, Capability capability)
    {
        _stream?.Write(span);
    }

    protected override bool UseAlreadyStartedProtection => true;

    protected override void StopForRecognitionModule()
    {
        if (_engine is not null)
        {
            _engine.RecognizeAsyncCancel();
            _engine.SpeechRecognized -= HandleSpeechRecognized;
            _engine.SpeechDetected -= HandleSpeechDetected;
            _engine.Dispose();
            _engine = null;
        }
        
        if (_mic is not null)
        {
            _mic.Stop();
            _mic.OnAudioProcessed -= HandleAudioProcessed;
            _mic.Dispose();
            _mic = null;
        }

        _stream?.Dispose();
        _stream = null;
    }

    protected override bool IsStarted()
        => _engine is not null || _stream is not null || _mic is not null;
    protected override bool IsProcessing()
            => _engine is not null && _stream is not null && _mic is not null && _mic.IsStarted; 
    #endregion

    #region Listening
    public override bool IsListening 
        => _mic?.IsListening ?? false;

    protected override bool SetListeningForRecognitionModule(bool state)
    {
        _mic!.SetListening(state);
        return IsListening;
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion
}