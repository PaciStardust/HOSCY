using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Misc;

namespace HoscyCoreTests.Mocks;

public class MockAfkService : IAfkService
{
    public bool Started { get; private set; } = false;
    public bool AfkRunning { get; private set; } = false;

    public ServiceStatus GetCurrentStatus()
    {
        return Started ?
            GetFaultIfExists() is null
            ? AfkRunning
                ? ServiceStatus.Processing
                : ServiceStatus.Started
            : ServiceStatus.Faulted
        : ServiceStatus.Stopped;
    }

    public Exception? GetFaultIfExists()
    {
        return null;
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    public void Start()
    {
        Started = true;
    }

    public void StartAfk()
    {
        if (Started)
            AfkRunning = true;
    }

    public void Stop()
    {
        StopAfk();
        Started = false;
    }

    public void StopAfk()
    {
        AfkRunning = false;
    }
}