using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc;

public interface IOscInternalControlService
{
    public bool HandleInternalControl(OscMessage message);
}