using HoscyCore.Utility;
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
    protected sealed override Res StopForService()
    {
        var result = StopForModule();
        OnModuleStopped.Invoke(this, EventArgs.Empty);
        return result;
    }

    protected abstract Res StopForModule();
    #endregion
}