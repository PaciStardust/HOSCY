using HoscyCore.Services.Core;

namespace HoscyCore.Services.Osc.SendReceive;

/// <summary>
/// Identifies an IOscListenService, a service used to receive OSC
/// </summary>
public interface IOscListenService : IAutoStartStopService
{
    public int? GetPort();
}