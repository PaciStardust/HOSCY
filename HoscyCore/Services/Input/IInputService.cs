using HoscyCore.Services.Core;
using HoscyCore.Services.Output.Core;

namespace HoscyCore.Services.Input;

public interface IInputService : IService
{
    public void SendManualMessage(string contents);
    public void SendExternalTextMessage(string contents);
    public void SendExternalTextNotification(string contents, OutputNotificationPriority prio = OutputNotificationPriority.Medium);
    public void SendExternalAudioMessage(string contents);
    public void SendExternalOtherMessage(string contents);
}