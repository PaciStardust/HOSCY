using System;

namespace Hoscy.Services.DependencyCore;

/// <summary>
/// Represents a service that can be started and stopped
/// </summary>
public interface IStartStopService : IService
{
    public void Start();
    public void Stop();
    public void Restart();
    public bool IsRunning();
    public StartStopStatus GetStatus();
    public Exception? GetFaultIfExists();
}

/// <summary>
/// Represents a StartStopService that will be launched automatically
/// </summary>
public interface IAutoStartStopService : IStartStopService; //todo: impl