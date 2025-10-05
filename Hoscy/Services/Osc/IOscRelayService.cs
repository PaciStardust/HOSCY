using Hoscy.Services.DependencyCore;
using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc;

/// <summary>
/// Relays incoming messages to other locations
/// </summary>
public interface IOscRelayService : IStartStopService //todo: impl
{
    /// <summary>
    /// Relays message to other locations
    /// </summary>
    public void HandleRelay(OscMessage message);
}