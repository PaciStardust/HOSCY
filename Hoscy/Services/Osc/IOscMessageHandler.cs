using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc;

/// <summary>
/// Represents a Message Handler for OSC Messages
/// </summary>
public interface IOscMessageHandler
{
    /// <summary>
    /// Handles an OscMessage
    /// </summary>
    /// <returns>True if handled</returns>
    public bool HandleMessage(OscMessage message);
}