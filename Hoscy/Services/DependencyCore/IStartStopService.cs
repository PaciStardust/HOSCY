using System;

namespace Hoscy.Services.DependencyCore;

public interface IStartStopService
{
    public void Start();
    public void Stop();
    public Exception? GetFaultIfExists();
    public bool IsRunning();

    public StatStopServiceStatus GetStatus()
    {
        if (!IsRunning()) return StatStopServiceStatus.Stopped;
        if (GetFaultIfExists() is not null) return StatStopServiceStatus.Faulted;
        return StatStopServiceStatus.Running;
    }
}

public enum StatStopServiceStatus {
    Running,
    Stopped,
    Faulted
}