using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Osc;

public interface IOscListenService : IStartStopService
{
    public bool TryRestart();
}