using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;

namespace HoscyCore.Services.Misc;

public interface IInputService : IService
{
    public void SendManualMessage(string contents);
    public void SendExternalMessage(string contents);
    public void SendExternalNotification(string contents, OutputNotificationPriority prio = OutputNotificationPriority.Medium);
}