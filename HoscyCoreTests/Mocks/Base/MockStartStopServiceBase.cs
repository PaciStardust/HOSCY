using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCoreTests.Mocks.Base;

public abstract class MockStartStopServiceBase : IStartStopService
{
    public bool Started { get; protected set; } = false;

    public virtual ServiceStatus GetCurrentStatus()
    {
        return Started ? (
            GetErrorMessageIfExists() is null
            ? ServiceStatus.Processing
            : ServiceStatus.Faulted
        )
        : ServiceStatus.Stopped;
    }

    public virtual ResMsg? GetErrorMessageIfExists()
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