using System;

namespace Hoscy.Services.DependencyCore;

public abstract class StartStopServiceBase : IStartStopService
{
    private Exception? _internalException = null;

    public void Start()
    {
        _internalException = null;
        StartInternal();
    }
    protected abstract void StartInternal();

    public abstract void Stop();
    public abstract bool IsRunning();

    public Exception? GetFaultIfExists()
        => _internalException;

    protected void SetFault(Exception ex)
    {
        _internalException = ex;
    }

    public StatStopServiceStatus GetStatus()
    {
        if (!IsRunning()) return StatStopServiceStatus.Stopped;
        if (GetFaultIfExists() is not null) return StatStopServiceStatus.Faulted;
        return StatStopServiceStatus.Running;
    }
}