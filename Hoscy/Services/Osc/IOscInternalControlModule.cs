using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc;

/// <summary>
/// Represents a Control Module for OscControl
/// </summary>
public interface IOscInternalControlModule
{
    public bool HandleMessage(OscMessage message);
}