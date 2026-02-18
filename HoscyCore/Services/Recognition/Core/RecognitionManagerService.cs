using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Recognition.Core;

public class RecognitionManagerService
(
    IBackToFrontNotifyService notify,
    ILogger logger,
    IContainerBulkLoader<IRecognitionModuleStartInfo> infoLoader,
    IContainerBulkLoader<IRecognitionModule> moduleLoader,
    ConfigModel config,
    OutputManagerService output
) 
    : StartStopModuleControllerBase<IRecognitionModuleStartInfo, IRecognitionModule>
        (notify, logger, infoLoader, moduleLoader),
    IRecognitionManagerService
{
    #region Injected
    private readonly ConfigModel _config = config;
    private readonly OutputManagerService _output = output;
    #endregion

    #region Module => Start / Stop
    protected override void OnModulePostStart(IRecognitionModule module)
    {
        module.OnSpeechActivity += HandleOnSpeechActivity;
        module.OnSpeechRecognized += HandleOnSpeechRecognized;

        bool listening = false;
        if (_config.Recognition_Mute_StartUnmuted)
        {
            listening = SetListeningInternal(module, true);
            if (!listening)
            {
                _logger.Warning("Failed to correctly set listening status on startup");
            }
        }
        InvokeModuleStatusChanged(listening, true);   
    }

    protected override void OnModulePreStop(IRecognitionModule module)
    {
        module.SetListening(false);

        InvokeModuleStatusChanged(false, false);
        module.OnSpeechActivity -= HandleOnSpeechActivity;
        module.OnSpeechRecognized -= HandleOnSpeechRecognized;
    }
    #endregion

    #region Module => Control
    public bool IsListening
        => _currentModule?.IsListening ?? false;

    public event EventHandler<RecognitionStatusChangedEventArgs> OnModuleStatusChanged = delegate {};
    private void InvokeModuleStatusChanged(bool listening, bool started)
    {
        _logger.Verbose("Triggering event for module status update started={started} listening={listening}", started, listening);
        OnModuleStatusChanged.Invoke(this, new(listening, started));
    }

    protected override string GetSelectedModuleName()
        => _config.Recognition_SelectedModuleName;

    public bool SetListening(bool state)
    {
        return SetListeningInternal(_currentModule, state);
    }
    public bool SetListeningInternal(IRecognitionModule? module, bool state)
    {
        _logger.Debug("Setting listening to {state}", state);

        bool newState;
        if (module is null)
        {
            _logger.Warning("Unable to set listening, provided module is null");
            newState = false;
        }
        else
        {
            newState = module.SetListening(state);
        }

        _logger.Debug("Listening was requested to be set to {state} and is now {actualState}", state, newState);
        return newState;
    }
    #endregion

    #region Functionality
    private void HandleOnSpeechRecognized(string rawOutput)
    {
        throw new NotImplementedException();
    }

    private void HandleOnSpeechActivity(bool state)
    {
        _logger.Verbose("Forwarding speech activity state {state} to output", state);
        _output.SetProcessingIndicator(state);
    }
    #endregion
}