using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyCore.Services.Interfacing;

/// <summary>
/// Implementation for IBackToFrontNotifyService
/// </summary>
/// <param name="logger"></param>
[LoadIntoDiContainer(typeof(IBackToFrontNotifyService), Lifetime.Singleton)]
public class BackToFrontNotifyService(ILogger logger) : IBackToFrontNotifyService
{
    private readonly ILogger _logger = logger.ForContext<BackToFrontNotifyService>();

    #region Events
    public event EventHandler<BackToFrontNotifyEventArgs> OnNotificationSent = delegate { };
    #endregion

    #region Invocation
    public void SendInfo(string title, string content = "", Exception? exception = null)
    {
        _logger.Verbose(exception, "Calling SendInfo with title {title} and content {content}", title, content);
        OnNotificationSent.Invoke(this, CreateArgs(BackToFrontNotifyLevel.Info, title, content, exception));
    }

    public void SendWarning(string title, string content = "", Exception? exception = null)
    {
        _logger.Verbose(exception, "Calling SendWarning with title {title} and content {content}", title, content);
        OnNotificationSent.Invoke(this, CreateArgs(BackToFrontNotifyLevel.Warning, title, content, exception));
    }

    public void SendError(string title, string content = "", Exception? exception = null)
    {
        _logger.Verbose(exception, "Calling SendError with title {title} and content {content}", title, content);
        OnNotificationSent.Invoke(this, CreateArgs(BackToFrontNotifyLevel.Error, title, content, exception));
    }

    public void SendFatal(string title, string content = "", Exception? exception = null)
    {
        _logger.Verbose(exception, "Calling SendFatal with title {title} and content {content}", title, content);
        OnNotificationSent.Invoke(this, CreateArgs(BackToFrontNotifyLevel.Fatal, title, content, exception));
    }
    #endregion

    #region Util
    private static BackToFrontNotifyEventArgs CreateArgs(BackToFrontNotifyLevel level, string title, string content, Exception? exception = null)
    {
        return new BackToFrontNotifyEventArgs(level, title, content, exception);
    }
    #endregion
}