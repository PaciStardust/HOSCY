namespace HoscyCore.Services.Core;

public interface IStartStopModuleController<TModuleStartInfo, TModule> : IStartStopService
{
    public IReadOnlyList<TModuleStartInfo> GetModuleInfos();
    public TModuleStartInfo? GetCurrentModuleInfo();
    public ServiceStatus GetCurrentModuleStatus();

    public void RefreshModuleSelection();
    public bool RestartCurrentModule();
}