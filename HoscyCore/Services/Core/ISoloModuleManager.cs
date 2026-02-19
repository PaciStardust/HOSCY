namespace HoscyCore.Services.Core;

public interface ISoloModuleManager<TModuleStartInfo, TModule> : IStartStopService
    where TModuleStartInfo : ISoloModuleStartInfo
    where TModule : IStartStopModule
{
    public IReadOnlyList<TModuleStartInfo> GetModuleInfos();
    public TModuleStartInfo? GetCurrentModuleInfo();
    public ServiceStatus GetCurrentModuleStatus();

    public void RefreshModuleSelection();
    public bool RestartCurrentModule();
}