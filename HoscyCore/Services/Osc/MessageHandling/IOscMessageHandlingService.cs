using HoscyCore.Services.Core;
using LucHeart.CoreOSC;

namespace HoscyCore.Services.Osc.MessageHandling;

/// <summary>
/// Service for OSC Message Handling
/// </summary>
public interface IOscMessageHandlingService : IAutoStartStopService
{
    public bool HandleMessage(OscMessage message);
}