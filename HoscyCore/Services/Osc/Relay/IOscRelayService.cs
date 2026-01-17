using HoscyCore.Services.DependencyCore;
using LucHeart.CoreOSC;

namespace HoscyCore.Services.Osc.Relay;

/// <summary>
/// Relays incoming messages to other locations
/// </summary>
public interface IOscRelayService : IAutoStartStopService
{
    /// <summary>
    /// Relays message to other locations
    /// </summary>
    public void HandleRelay(OscMessage message);

    /// <summary>
    /// Displays names of invalid filters
    /// </summary>
    /// <returns></returns>
    public string[] GetInvalidFilterNames();
}