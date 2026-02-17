using HoscyCore.Services.Core;
using LucHeart.CoreOSC;

namespace HoscyCore.Services.Osc.MessageHandling;

/// <summary>
/// Represents a Message Handler for OSC Messages
/// </summary>
public interface IOscMessageHandler : IService
{
    /// <summary>
    /// Handles an OscMessage
    /// </summary>
    /// <returns>True if handled</returns>
    public bool HandleMessage(OscMessage message);
}