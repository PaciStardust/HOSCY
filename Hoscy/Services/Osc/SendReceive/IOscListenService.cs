using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Osc.SendReceive;

/// <summary>
/// Identifies an IOscListenService, a service used to receive OSC
/// </summary>
public interface IOscListenService : IStartStopService
{
    public int? GetPort();
}