using HoscyCore.Services.Dependency;

namespace HoscyCoreTests.Mocks;

public abstract class MockStartStopServiceBase : IStartStopService //todo: [REFACTOR++] Should this be done differently?
{
    public bool Started { get; protected set; } = false;

    public virtual ServiceStatus GetCurrentStatus()
    {
        return Started ? (
            GetFaultIfExists() is null
            ? ServiceStatus.Processing
            : ServiceStatus.Faulted
        )
        : ServiceStatus.Stopped;
    }

    public virtual Exception? GetFaultIfExists()
        => null;

    public virtual void Restart()
    {
        Stop();
        Start();
    }
    public virtual void Start()
    {
        Started = true;
    }
    public virtual void Stop()
    {
        Started = false;
    }
}