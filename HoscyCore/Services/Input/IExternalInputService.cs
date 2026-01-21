using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;

namespace HoscyCore.Services.Input;

public interface IExternalInputService : IService
{
    public void SendMessage(string contents);
    public void SendNotification(string contents, OutputNotificationPriority prio = OutputNotificationPriority.Medium);
}