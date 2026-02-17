using HoscyCore.Services.Core;

namespace HoscyCore.Services.Misc;

public interface IAfkService : IAutoStartStopService
{
    public void StartAfk();
    public void StopAfk();
}