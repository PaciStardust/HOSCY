using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Speech.Recognition;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Services.Translation.Core;
using Serilog;

namespace HoscyCore.Services.Recognition.Modules;

[SupportedOSPlatform("windows")]
[PrototypeLoadIntoDiContainer(typeof(WindowsRecognitionModuleStartInfo), Lifetime.Singleton, SupportedPlatformFlags.Windows)]
public class WindowsRecognitionModuleStartInfo : ITranslationModuleStartInfo
{
    public WindowsRecognitionModuleStartInfo()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("Module is only supported on Windows");
        }
    }

    public string Name => "Windows Recognizer";
    public string Description => "Recognizer using Windows Recognition, low quality, please avoid";
    public Type ModuleType => typeof(WindowsRecognitionModule);

    public TranslationModuleConfigFlags ConfigFlags 
        => TranslationModuleConfigFlags.Windows;
}

[SupportedOSPlatform("windows")]
[PrototypeLoadIntoDiContainer(typeof(WindowsRecognitionModule), Lifetime.Transient, SupportedPlatformFlags.Windows)]
public class WindowsRecognitionModule: StartStopModuleBase, IRecognitionModule
{
    #region Vars
    public WindowsRecognitionModule(ILogger logger, ConfigModel config)
        : base(logger.ForContext<WindowsRecognitionModule>())
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("Module is only supported on Windows");
        }

        _config = config;
    }

    private readonly ConfigModel _config;

    private SpeechRecognitionEngine? _engine = null!;
    #endregion

    #region Rec Vars
    public bool IsListening { get; private set; } = false;

    public event Action<string> OnSpeechRecognized = delegate { };
    public event Action<bool> OnSpeechActivity = delegate { };
    #endregion

    #region Start / Stop
    protected override void StartInternal()
    {
        //todo: [FEAT] Logging?
        var engine = CreateEngine();
        engine.LoadGrammar(new DictationGrammar());
        engine.SpeechDetected += HandleSpeechDetected;
        engine.SpeechRecognized += HandleSpeechRecognized;
        engine.SetInputToDefaultAudioDevice(); //todo: [FEAT] Can switch to another mic?
        _engine = engine;
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override void StopInternalInternal()
    {
        //todo: [REFACTOR] Should listening be stopped here?
        _engine?.SpeechRecognized -= HandleSpeechRecognized;
        _engine?.SpeechDetected -= HandleSpeechDetected;
        _engine?.Dispose();
    }

    protected override bool IsStarted()
    {
        return _engine is not null;
    }

    protected override bool IsProcessing()
    {
        return IsStarted() && IsListening; 
    }

    public bool SetListening(bool state)
    {
        if (_engine is null) return false;
        if (state == IsListening) return state;

        _logger.Debug("Changing listening state to {requestedState}", state);
        try
        {
            if (state)
            {
                _logger.Verbose("Starting RecognizeAsync");
                _engine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {
                _logger.Verbose("Stopping RecognizeAsync");
                _engine.RecognizeAsyncStop();
            }
            IsListening = state;
            _logger.Debug("Changed listening state to {requestedState}", state);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed changing listening state to {requestedState}", state);
            SetFault(ex); //todo: [REFACTOR] Should set fault not also provide a message maybe?
        }

        return IsListening;
    }
    #endregion

    #region Functionality
    private SpeechRecognitionEngine CreateEngine()
    {
        _logger.Debug("Creating new windows speech recognition engine");
        try
        {
            return new(_config.Recognition_Windows_ModelId);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Unable to instantiate engine with provided model id {modelId}, trying without", 
                _config.Recognition_Windows_ModelId); //todo: [FEAT] Actually use notify to send non-errors
            return new();
        }
    }

    private void HandleSpeechDetected(object? sender, SpeechDetectedEventArgs e)
    {
        _logger.Verbose("Received speech activity");
        OnSpeechActivity.Invoke(true);
    }

    private void HandleSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        _logger.Verbose("Received speech recognized: \"{text}\"", e.Result.Text);
        OnSpeechActivity.Invoke(false);
        OnSpeechRecognized.Invoke(e.Result.Text);
    }
    #endregion
}