using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Recognition.Core;

public abstract class RecognitionModuleBase(ILogger logger)
    : StartStopModuleBase(logger), IRecognitionModule
{
    #region Events
    public event Action<string> OnSpeechRecognized = delegate { };
    protected void InvokeSpeechRecognized(string recognizedText)
        => OnSpeechRecognized.Invoke(recognizedText);

    public event Action<bool> OnSpeechActivity = delegate { };
    protected void InvokeSpeechActivity(bool state)
        => OnSpeechActivity.Invoke(state);

    public event Action OnInternalListeningStatusChange = delegate { };
    protected void InvokeInternalListeningStatusChange()
        => OnInternalListeningStatusChange.Invoke();
    #endregion

    #region Listening
    public abstract bool IsListening { get; }

    public Res<bool> SetListening(bool state)
    {
        _logger.Debug("Setting listening status to {state}", state);

        if (!IsStarted() && UseOnlySetListeningWhenStartedProtection)
            return ResC.TFailLog<bool>("Listening status not set, module is not started", _logger, lvl: ResMsgLvl.Warning);

        if (state == IsListening)
        {
            _logger.Debug("Listening status already in requested state, not changing");
            return ResC.TOk(state);
        }

        var res = SetListeningForRecognitionModule(state);
        _logger.Debug("Set listening status to {newState} (requested={requestedState})", res, state);
        return res;
    }
    protected abstract Res<bool> SetListeningForRecognitionModule(bool state);
    protected abstract bool UseOnlySetListeningWhenStartedProtection { get; }
    #endregion

    #region Stop
    protected sealed override Res StopForModule()
    {
        var resListening = SetListening(false);
        var resRecognition = StopForRecognitionModule();
        return resRecognition.IsOk ? ResC.Ok() : ResC.FailM(resListening.Msg?.WithContext("Listening"), resRecognition.Msg?.WithContext("Stop"));
    }
    protected abstract Res StopForRecognitionModule();
    #endregion
}