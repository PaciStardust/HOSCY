using Serilog;

namespace HoscyCore.Services.Core;

public abstract class StartStopModuleBase(ILogger logger) : StartStopServiceBase(logger), IStartStopModule
{
    #region Events
    public event EventHandler<Exception> OnRuntimeError = delegate { };
    public event EventHandler OnModuleStopped = delegate { };
    #endregion

    #region Fault Handling
    protected override void SetFault(Exception? ex)
    {
        base.SetFault(ex);
        if (ex is not null)
        {
            OnRuntimeError.Invoke(this, ex);
        }
    }
    #endregion

    #region Start / Stop
    protected override void StopInternal()
    {
        StopInternalInternal();
        OnModuleStopped.Invoke(this, EventArgs.Empty);
    }

    // Yes this is horribly named, I am aware
    protected abstract void StopInternalInternal();
    #endregion
}