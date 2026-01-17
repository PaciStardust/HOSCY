namespace HoscyCore.Services.DependencyCore;

/// <summary>
/// Represents a service that can be started and stopped
/// </summary>
public interface IStartStopService : IService
{
    public void Start();
    public void Stop();
    public void Restart();
    public bool IsRunning(); //todo: should this differentiate between "running" and "actually doing something"?
    public StartStopStatus GetStatus();
    public Exception? GetFaultIfExists();
}

/// <summary>
/// Represents a StartStopService that will be launched automatically
/// </summary>
public interface IAutoStartStopService : IStartStopService;