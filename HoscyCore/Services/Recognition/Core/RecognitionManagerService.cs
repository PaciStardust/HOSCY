using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Interfacing;
using Serilog;

namespace HoscyCore.Services.Recognition.Core;

public class RecognitionManagerService
(
    IBackToFrontNotifyService notify,
    ILogger logger,
    IContainerBulkLoader<IRecognitionModuleStartInfo> infoLoader,
    IContainerBulkLoader<IRecognitionModule> moduleLoader,
    ConfigModel config
) 
    : StartStopModuleControllerBase<IRecognitionModuleStartInfo, IRecognitionModule>
        (notify, logger, infoLoader, moduleLoader),
    IRecognitionManagerService
{
    #region Injected
    private readonly ConfigModel _config = config;
    #endregion

    #region Module => Start / Stop
    protected override void OnModulePostStart(IRecognitionModule module)
    {
        module.OnSpeechActivity += HandleOnSpeechActivity;
        module.OnSpeechRecognized += HandleOnSpeechRecognized;
        InvokeModuleStatusChanged(false, true);
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
        _logger.Debug("Setting listening indicator to {state}", state);

        bool newState;
        if (_currentModule is null)
        {
            _logger.Warning("Unable to set listening indicator, no module is loaded");
            newState = false;
        }
        else
        {
            newState = _currentModule.SetListening(state);
        }

        _logger.Debug("Listening indicator was requested to be set to {state} and is now {actualState}", state, newState);
        return newState;
    }
    #endregion

    #region Functionality
    private void HandleOnSpeechRecognized(string obj)
    {
        throw new NotImplementedException();
    }

    private void HandleOnSpeechActivity(bool obj)
    {
        throw new NotImplementedException();
    }
    #endregion
}