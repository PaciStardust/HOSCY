using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc;

/// <summary>
/// Service for OSC Message Handling
/// </summary>
public interface IOscMessageHandlingService
{
    public bool HandleMessage(OscMessage message);
}