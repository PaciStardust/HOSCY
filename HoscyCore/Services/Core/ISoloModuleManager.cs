using HoscyCore.Utility;

namespace HoscyCore.Services.Core;

public interface ISoloModuleManager<TModuleStartInfo> : IAutoStartStopService
    where TModuleStartInfo : ISoloModuleStartInfo
{
    public IReadOnlyList<TModuleStartInfo> GetModuleInfos();
    public Res<TModuleStartInfo>? GetCurrentModuleInfo();
    public ServiceStatus GetCurrentModuleStatus();
    public bool IsModuleRefreshNeeded();

    public Res StartModule();
    public Res RestartModule();
    public Res StopModule();
    public Res RefreshModule();
}