using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc;

/// <summary>
/// Represents a Message Handler for OSC Messages
/// </summary>
public interface IOscMessageHandler
{
    public bool HandleMessage(OscMessage message);
}