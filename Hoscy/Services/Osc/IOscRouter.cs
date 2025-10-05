using Hoscy.Services.DependencyCore;
using LucHeart.CoreOSC;

namespace Hoscy.Services.Osc;

public interface IOscRouter : IStartStopService
{
    public void HandleRouting(OscMessage message);
}