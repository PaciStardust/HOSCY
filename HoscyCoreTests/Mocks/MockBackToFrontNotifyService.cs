using HoscyCore.Services.Interfacing;

namespace HoscyCoreTests.Mocks;

public class MockBackToFrontNotifyService : IBackToFrontNotifyService
{
    public event EventHandler<BackToFrontNotifyEventArgs> OnInfo = delegate { };
    public event EventHandler<BackToFrontNotifyEventArgs> OnWarning = delegate { };
    public event EventHandler<BackToFrontNotifyEventArgs> OnError = delegate { };
    public event EventHandler<BackToFrontNotifyEventArgs> OnFatal = delegate { };

    public readonly List<(string Title, string Content, Exception? Ex)> Fatals = [];
    public readonly List<(string Title, string Content, Exception? Ex)> Errors = [];
    public readonly List<(string Title, string Content, Exception? Ex)> Warnings = [];
    public readonly List<(string Title, string Content, Exception? Ex)> Infos = [];

    public void SendError(string title, string content = "", Exception? exception = null)
    {
        Errors.Add((title, content, exception));
        OnError?.Invoke(this, new BackToFrontNotifyEventArgs(title, content, exception));
    }

    public void SendFatal(string title, string content = "", Exception? exception = null)
    {
        Fatals.Add((title, content, exception));
        OnFatal?.Invoke(this, new BackToFrontNotifyEventArgs(title, content, exception));
    }

    public void SendInfo(string title, string content = "", Exception? exception = null)
    {
        Infos.Add((title, content, exception));
        OnInfo?.Invoke(this, new BackToFrontNotifyEventArgs(title, content, exception));
    }

    public void SendWarning(string title, string content = "", Exception? exception = null)
    {
        Warnings.Add((title, content, exception));
        OnWarning?.Invoke(this, new BackToFrontNotifyEventArgs(title, content, exception));
    }
}