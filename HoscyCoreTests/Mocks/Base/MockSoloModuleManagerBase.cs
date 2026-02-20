using HoscyCore.Services.Core;

namespace HoscyCoreTests.Mocks.Base;

public abstract class MockSoloModuleManagerBase<TModuleStartInfo> : MockStartStopServiceBase, ISoloModuleManager<TModuleStartInfo>
    where TModuleStartInfo : class, ISoloModuleStartInfo
{
    public ServiceStatus CurrentModuleStatus { get; set; } = ServiceStatus.Processing;
    public ServiceStatus GetCurrentModuleStatus()
    {
        return CurrentModuleStatus;
    }

    public TModuleStartInfo? GetCurrentModuleInfo()
    {
        return null;
    }

    public IReadOnlyList<TModuleStartInfo> GetModuleInfos()
    {
        return [];
    }

    public bool StartModule()
    {
        return true;
    }

    public bool RestartModule()
    {
        return true;
    }

    public bool StopModule()
    {
        return true;
    }
}