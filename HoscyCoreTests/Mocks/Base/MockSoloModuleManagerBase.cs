using HoscyCore.Services.Core;

namespace HoscyCoreTests.Mocks.Base;

public abstract class MockSoloModuleManagerBase<TModuleStartInfo> : MockStartStopServiceBase
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

    public void RefreshModuleSelection()
    {
        return;
    }
    
    public bool RestartCurrentModule()
    {
        return true;
    }
}