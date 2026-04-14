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
public class WindowsRecognitionModule : WindowsRecognitionModuleBase
{
    #region Vars
    public WindowsRecognitionModule(ILogger logger, ConfigModel config, IRecognitionModelProviderService modelProvider)
        : base(logger.ForContext<WindowsRecognitionModule>(), config, modelProvider)
    {
        OtherUtils.ThrowOnInvalidPlatform([OSPlatform.Windows]);
    }

    private SpeechRecognitionEngine? _engine = null!;
    #endregion

    #region Start / Stop
    protected override Res StartForService()
    {
        var engineResult = CreateEngine();
        if (!engineResult.IsOk) return ResC.Fail(engineResult.Msg); 

        _engine = engineResult.Value;
        _engine.LoadGrammar(new DictationGrammar());
        _engine.SpeechDetected += HandleSpeechDetected;
        _engine.SpeechRecognized += HandleSpeechRecognized;

        var audioSetResult = ResC.WrapR(_engine.SetInputToDefaultAudioDevice, "Failed to set audio device", _logger);
        return audioSetResult;
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForRecognitionModule()
    {
        if (_engine is not null)
        {
            var res = ResC.WrapR(_engine.RecognizeAsyncCancel, "Failed to cancel recognition", _logger);
            _engine.SpeechRecognized -= HandleSpeechRecognized;
            _engine.SpeechDetected -= HandleSpeechDetected;
            return res;
        }
        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _engine?.Dispose();
        _engine = null;
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

    protected override Res<bool> SetListeningForRecognitionModule(bool state)
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
            SetFault(ex); //todo: [REFACTOR] Should set fault not also provide a message maybe, also warnings or also just set fault globally?
            return ResC.TFailLog<bool>($"Failed changing listening state to {state}", _logger, ex);
        }

        return ResC.TOk(IsListening);
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion
}