using Hoscy.Services.DependencyCore;
using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc.MessageHandling;

/// <summary>
/// Service for OSC Message Handling
/// </summary>
public interface IOscMessageHandlingService : IService
{
    public bool HandleMessage(OscMessage message);
}