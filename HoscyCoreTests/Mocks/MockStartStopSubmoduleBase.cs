using HoscyCore.Services.DependencyCore;

namespace HoscyCoreTests.Mocks;

public abstract class MockStartStopSubmoduleBase : MockStartStopServiceBase, IStartStopSubmodule
{    
    public Exception? ExceptionToThrow { get; set; } = null;

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

    public override void Start()
    {
        ThrowIfNeeded();
        base.Start();
    }
    public override void Stop()
    {
        base.Stop();
        ThrowIfNeeded();
        OnSubmoduleStopped.Invoke(this, EventArgs.Empty);
    }
    public override void Restart()
    {
        ThrowIfNeeded();
        base.Restart();
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

    private void ThrowIfNeeded()
    {
        if (ExceptionToThrow is null) return;
        throw ExceptionToThrow;
    }

    public virtual void ResetStats()
    {
        ExceptionToThrow = null;
        _fault = null;
        OverrideRunningStatus = null;
    }
}