using HoscyCore.Services.DependencyCore;

namespace HoscyCoreTests.Mocks;

public abstract class MockStartStopSubmoduleBase : MockStartStopServiceBase, IStartStopSubmodule
{
    public event EventHandler<Exception> OnRuntimeError = delegate { };
    public event EventHandler OnSubmoduleStopped = delegate { };

    public ServiceStatus? OverrideRunningStatus { get; set; } = null;
    public override ServiceStatus GetCurrentStatus()
    {
        if (Started)
        {
            return OverrideRunningStatus ?? ServiceStatus.Processing;
        }
        return ServiceStatus.Stopped;
    }

    public override void Stop()
    {
        base.Stop();
        OnSubmoduleStopped.Invoke(this, EventArgs.Empty);
    }

    protected Exception? _fault = null;
    public override Exception? GetFaultIfExists()
    {
        return _fault;
    }
    public void InduceError(Exception? ex)
    {
        _fault = ex;
        if (ex is not null)
        {
            OnRuntimeError.Invoke(this, ex);
        }
    }

    public virtual void ResetStats()
    {
        _fault = null;
        OverrideRunningStatus = null;
    }
}