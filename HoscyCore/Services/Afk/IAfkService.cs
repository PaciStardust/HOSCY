using HoscyCore.Services.Core;

namespace HoscyCore.Services.Afk;

public interface IAfkService : IAutoStartStopService
{
    public void StartAfk();
    public void StopAfk();
}