using HoscyCore.Services.Interfacing;

namespace HoscyCoreTests.Mocks;

public class MockBackToFrontNotifyService : IBackToFrontNotifyService
{
    public event EventHandler<BackToFrontNotifyEventArgs> OnNotificationSent = delegate { };

    public readonly List<(string Title, string Content, Exception? Ex)> Fatals = [];
    public readonly List<(string Title, string Content, Exception? Ex)> Errors = [];
    public readonly List<(string Title, string Content, Exception? Ex)> Warnings = [];
    public readonly List<(string Title, string Content, Exception? Ex)> Infos = [];

    public void SendError(string title, string content = "", Exception? exception = null)
    {
        Errors.Add((title, content, exception));
        OnNotificationSent?.Invoke(this, new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Error, title, content, exception));
    }

    public void SendFatal(string title, string content = "", Exception? exception = null)
    {
        Fatals.Add((title, content, exception));
        OnNotificationSent?.Invoke(this, new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Fatal, title, content, exception));
    }

    public void SendInfo(string title, string content = "", Exception? exception = null)
    {
        Infos.Add((title, content, exception));
        OnNotificationSent?.Invoke(this, new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Info, title, content, exception));
    }

    public void SendWarning(string title, string content = "", Exception? exception = null)
    {
        Warnings.Add((title, content, exception));
        OnNotificationSent?.Invoke(this, new BackToFrontNotifyEventArgs(BackToFrontNotifyLevel.Warning, title, content, exception));
    }
}