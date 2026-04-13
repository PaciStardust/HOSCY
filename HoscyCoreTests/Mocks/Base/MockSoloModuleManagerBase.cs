using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCoreTests.Mocks.Base;

public abstract class MockSoloModuleManagerBase<TModuleStartInfo> : MockStartStopServiceBase, ISoloModuleManager<TModuleStartInfo>
    where TModuleStartInfo : class, ISoloModuleStartInfo
{
    public ServiceStatus CurrentModuleStatus { get; set; } = ServiceStatus.Processing;
    public ServiceStatus GetCurrentModuleStatus()
    {
        return CurrentModuleStatus;
    }

    public Res<TModuleStartInfo> GetCurrentModuleInfo()
    {
        return ResC.TFail<TModuleStartInfo>(ResMsg.Err("No module available"));
    }

    public IReadOnlyList<TModuleStartInfo> GetModuleInfos()
    {
        return [];
    }

    public Res StartModule()
    {
        return ResC.Ok();
    }

    public Res RestartModule()
    {
        return ResC.Ok();
    }

    public Res StopModule()
    {
        return ResC.Ok();
    }
}