using HoscyCore.Services.Interfacing;

namespace HoscyCoreTests.Mocks;

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
}