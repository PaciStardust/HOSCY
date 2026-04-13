using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;

namespace HoscyCoreTests.Mocks.Impl;

public class MockBackToFrontNotifyService : IBackToFrontNotifyService
{
    public event EventHandler<BackToFrontNotifyEventArgs> OnNotificationSent = delegate { };

    public readonly List<BackToFrontNotifyEventArgs> Notifications = [];

    public void SendError(string title, string content = "", Exception? exception = null)
    {
        var args = new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Error, title, content, exception);
        Notifications.Add(args);
        OnNotificationSent?.Invoke(this, args);
    }

    public void SendFatal(string title, string content = "", Exception? exception = null)
    {
        var args = new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Fatal, title, content, exception);
        Notifications.Add(args);
        OnNotificationSent?.Invoke(this, args);
    }

    public void SendInfo(string title, string content = "", Exception? exception = null)
    {
        var args = new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Info, title, content, exception);
        Notifications.Add(args);
        OnNotificationSent?.Invoke(this, args);
    }

    public void SendWarning(string title, string content = "", Exception? exception = null)
    {
        var args = new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Warning, title, content, exception);
        Notifications.Add(args);
        OnNotificationSent?.Invoke(this, args);
    }

    public void SendResult(string title, ResMsg resMsg)
    {
        var args = new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Error, title, resMsg.Message, null);
        Notifications.Add(args);
        OnNotificationSent?.Invoke(this, args);
    }
}