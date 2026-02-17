namespace HoscyCore.Services.DependencyCore;

public abstract class StartStopModuleBase : StartStopServiceBase, IStartStopModule
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
    public override void Stop()
    {
        StopInternal();
        OnModuleStopped.Invoke(this, EventArgs.Empty);
    }

    protected abstract void StopInternal();
    #endregion
}