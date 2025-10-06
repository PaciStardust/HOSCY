using System;

namespace Hoscy.Services.DependencyCore;

/// <summary>
/// Represents a service that can be started and stopped
/// </summary>
public interface IStartStopService : IDisposable
{
    public void Start();
    public void Stop();
    public bool TryRestart();
    public bool IsRunning();
    public StatStopServiceStatus GetStatus();
    public Exception? GetFaultIfExists();

    void IDisposable.Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}

public enum StatStopServiceStatus {
    Running,
    Stopped,
    Faulted
}