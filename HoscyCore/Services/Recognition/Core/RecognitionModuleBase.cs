using HoscyCore.Services.Core;
using Serilog;

namespace HoscyCore.Services.Recognition.Core;

public abstract class RecognitionModuleBase(ILogger logger)
    : StartStopModuleBase(logger), IRecognitionModule
{
    #region Events
    public event Action<string> OnSpeechRecognized = delegate { };
    public event Action<bool> OnSpeechActivity = delegate { };
    #endregion

    #region Listening
    public abstract bool IsListening { get; }

    public bool SetListening(bool state)
    {
        _logger.Debug("Setting listening status to {state}", state);

        if (!IsStarted() && UseOnlySetListeningWhenStartedProtection)
        {
            _logger.Debug("Listening status not set, module is not started");
            return false;
        }

        if (state == IsListening)
        {
            _logger.Debug("Listening status already in requested state, not changing");
            return state;
        }

        var res = SetListeningForModule(state);
        _logger.Debug("Set listening status to {newState} (requested={requestedState})", res, state);
        return state;
    }
    protected abstract bool SetListeningForModule(bool state);
    protected abstract bool UseOnlySetListeningWhenStartedProtection { get; }
    #endregion

    #region Stop
    protected sealed override void StopForModule()
    {
        SetListening(false);
        StopForRecognitionModule();
    }
    protected abstract void StopForRecognitionModule();
    #endregion
}