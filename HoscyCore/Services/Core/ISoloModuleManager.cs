namespace HoscyCore.Services.Core;

public interface ISoloModuleManager<TModuleStartInfo> : IAutoStartStopService
    where TModuleStartInfo : ISoloModuleStartInfo
{
    public IReadOnlyList<TModuleStartInfo> GetModuleInfos();
    public TModuleStartInfo? GetCurrentModuleInfo();
    public ServiceStatus GetCurrentModuleStatus();

    public bool StartModule();
    public bool RestartModule();
    public bool StopModule();
}