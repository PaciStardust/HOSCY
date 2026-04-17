using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCoreTests.Mocks.Base;

public abstract class MockStartStopModuleBase : MockStartStopServiceBase, IStartStopModule
{    
    public Res? ResultToReturn { get; set; } = null;

    public event EventHandler<ResMsg> OnRuntimeError = delegate { };
    public event EventHandler OnModuleStopped = delegate { };

    public ServiceStatus? OverrideRunningStatus { get; set; } = null;
    public override ServiceStatus GetCurrentStatus()
    {
        if (Started)
        {
            return OverrideRunningStatus ?? ServiceStatus.Processing;
        }
        return ServiceStatus.Stopped;
    }

    public override Res Start()
    {
        return ResultToReturn ?? base.Start();
    }
    public override Res Stop()
    {
        var res = base.Stop();
        OnModuleStopped.Invoke(this, EventArgs.Empty);
        return ResultToReturn ?? res;
    }
    public override Res Restart()
    {
        return ResultToReturn ?? base.Restart();
    }

    protected ResMsg? _fault = null;
    public override ResMsg? GetErrorMessageIfExists()
    {
        return _fault;
    }
    public void InduceError(ResMsg? msg)
    {
        _fault = msg;
        if (msg is not null)
        {
            OnRuntimeError.Invoke(this, msg);
        }
    }

    public virtual void ResetStats()
    {
        ResultToReturn = null;
        _fault = null;
        OverrideRunningStatus = null;
    }
}