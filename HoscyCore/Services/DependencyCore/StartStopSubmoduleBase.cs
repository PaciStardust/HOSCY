using System;

namespace HoscyCore.Services.DependencyCore;

public abstract class StartStopSubmoduleBase<Tidentifier> : StartStopServiceBase, IStartStopSubmodule<Tidentifier>
{
    #region Events
    public event EventHandler<Exception> OnRuntimeError = delegate { };
    public event EventHandler OnSubmoduleStopped = delegate { };
    #endregion

    #region Info & Status
    public abstract Tidentifier GetIdentifier();
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
    public override void Stop()
    {
        StopInternal();
        OnSubmoduleStopped.Invoke(this, EventArgs.Empty);
    }

    protected abstract void StopInternal();
    #endregion
}