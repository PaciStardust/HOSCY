using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc;

/// <summary>
/// Service for routing OSC to internal tooling
/// </summary>
public interface IOscInternalControlService
{
    public bool HandleInternalControl(OscMessage message);
}