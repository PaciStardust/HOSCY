using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Misc;

public interface IAfkService : IAutoStartStopService
{
    public void StartAfk();
    public void StopAfk();
}