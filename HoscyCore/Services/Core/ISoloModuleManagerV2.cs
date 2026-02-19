namespace HoscyCore.Services.Core;

public interface ISoloModuleManagerV2<TModuleStartInfo> : IStartStopService
    where TModuleStartInfo : ISoloModuleStartInfo
{
    public IReadOnlyList<TModuleStartInfo> GetModuleInfos();
    public TModuleStartInfo? GetCurrentModuleInfo();
    public ServiceStatus GetCurrentModuleStatus();

    public bool StartModule();
    public bool RestartModule();
    public bool StopModule();
}