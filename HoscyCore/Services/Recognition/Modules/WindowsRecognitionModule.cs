using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Speech.Recognition;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Recognition.Modules;

[SupportedOSPlatform("windows")]
[PrototypeLoadIntoDiContainer(typeof(WindowsRecognitionModuleStartInfo), Lifetime.Singleton, SupportedPlatformFlags.Windows)]
public class WindowsRecognitionModuleStartInfo : IRecognitionModuleStartInfo
{
    public WindowsRecognitionModuleStartInfo()
    {
        OtherUtils.ThrowOnInvalidPlatform([OSPlatform.Windows]);
    }

    public string Name => "Windows Recognizer";
    public string Description => "Recognizer using Windows Recognition, low quality, please avoid";
    public Type ModuleType => typeof(WindowsRecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Windows;
}

[SupportedOSPlatform("windows")]
[PrototypeLoadIntoDiContainer(typeof(WindowsRecognitionModule), Lifetime.Transient, SupportedPlatformFlags.Windows)]
public class WindowsRecognitionModule : RecognitionModuleBase
{
    #region Vars
    public WindowsRecognitionModule(ILogger logger, ConfigModel config, IRecognitionModelProviderService modelProvider)
        : base(logger.ForContext<WindowsRecognitionModule>())
    {
        OtherUtils.ThrowOnInvalidPlatform([OSPlatform.Windows]);

        _config = config;
        _modelProvider = modelProvider;
    }

    private readonly ConfigModel _config;
    private readonly IRecognitionModelProviderService _modelProvider;
    private SpeechRecognitionEngine? _engine = null!;
    #endregion

    #region Start / Stop
    protected override void StartForService()
    {
        var engine = CreateEngine();
        engine.LoadGrammar(new DictationGrammar());
        engine.SpeechDetected += HandleSpeechDetected;
        engine.SpeechRecognized += HandleSpeechRecognized;
        engine.SetInputToDefaultAudioDevice(); //todo: [FEAT] Can switch to another mic?
        _engine = engine;
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override void StopForRecognitionModule()
    {
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
    #endregion

    #region Listening
    public override bool IsListening => _isListening;
    private bool _isListening = false;

    protected override bool SetListeningForRecognitionModule(bool state)
    {
        try
        {
            if (state)
            {
                _logger.Verbose("Starting RecognizeAsync");
                _engine!.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {
                _logger.Verbose("Stopping RecognizeAsync");
                _engine!.RecognizeAsyncStop();
            }
            _isListening = state;
            _logger.Debug("Changed listening state to {requestedState}", state);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed changing listening state to {requestedState}", state);
            SetFault(ex); //todo: [REFACTOR] Should set fault not also provide a message maybe?
        }

        return IsListening;
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion

    #region Functionality
    private SpeechRecognitionEngine CreateEngine()
    {
        _logger.Debug("Creating new windows speech recognition engine");

        var recognizerInfo = _modelProvider.GetWindowsRecognizers()
            .Where(x => x.Id == _config.Recognition_Windows_ModelId)
            .ToArray();

        if (recognizerInfo.Length == 0)
        {
            _logger.Warning("Unable to instantiate engine with provided model id {modelId}, trying without", 
                _config.Recognition_Windows_ModelId);
            return new();
        } else if (recognizerInfo.Length > 1)
        {
            _logger.Warning("Multiple matching infos found for model id {modelId}, picking first", _config.Recognition_Windows_ModelId);
        }

        return new(recognizerInfo[0]);
    }

    private void HandleSpeechDetected(object? sender, SpeechDetectedEventArgs e)
    {
        _logger.Verbose("Received speech activity");
        InvokeSpeechActivity(true);
    }

    private void HandleSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        _logger.Verbose("Received speech recognized: \"{text}\"", e.Result.Text);
        InvokeSpeechActivity(false);
        InvokeSpeechRecognized(e.Result.Text);
    }
    #endregion
}