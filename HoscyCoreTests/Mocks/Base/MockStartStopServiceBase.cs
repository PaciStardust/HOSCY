using HoscyCore.Services.Core;
using HoscyCore.Utility;

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

    public virtual Res Restart()
    {
        var res = Stop();
        if (!res.IsOk) return res;
        return Start();
    }
    public virtual Res Start()
    {
        Started = true;
        return ResC.Ok();
    }
    public virtual Res Stop()
    {
        Started = false;
        return ResC.Ok();
    }
}