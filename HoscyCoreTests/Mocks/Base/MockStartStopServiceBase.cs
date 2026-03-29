using HoscyCore.Services.Core;

namespace HoscyCoreTests.Mocks.Base;

public abstract class MockStartStopServiceBase : IStartStopService
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