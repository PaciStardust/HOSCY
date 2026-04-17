using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Core;

public abstract class StartStopModuleBase(ILogger logger) : StartStopServiceBase(logger), IStartStopModule
{
    #region Events
    public event EventHandler<ResMsg> OnRuntimeError = delegate { };
    public event EventHandler OnModuleStopped = delegate { };
    #endregion

    #region Fault Handling
    protected override void SetFault(ResMsg? msg)
    {
        base.SetFault(msg);
        if (msg is not null)
        {
            OnRuntimeError.Invoke(this, msg);
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