using System;

namespace Hoscy.Services.DependencyCore;

public interface IStartStopService
{
    public void Start();
    public void Stop();
    public bool IsRunning();
    public StatStopServiceStatus GetStatus();
    public Exception? GetFaultIfExists();
}

public enum StatStopServiceStatus {
    Running,
    Stopped,
    Faulted
}