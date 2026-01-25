using HoscyCore.Services.DependencyCore;
using Serilog;

namespace HoscyCore.Services.Interfacing;

/// <summary>
/// Implementation for IBackToFrontNotifyService
/// </summary>
/// <param name="logger"></param>
[LoadIntoDiContainer(typeof(IBackToFrontNotifyService), Lifetime.Singleton)]
public class BackToFrontNotifyService(ILogger logger) : IBackToFrontNotifyService //todo: [FEAT] Make use of this
{
    private readonly ILogger _logger = logger.ForContext<BackToFrontNotifyService>();

    #region Events
    public event EventHandler<BackToFrontNotifyEventArgs> OnInfo = delegate { };
    public event EventHandler<BackToFrontNotifyEventArgs> OnWarning = delegate { };
    public event EventHandler<BackToFrontNotifyEventArgs> OnError = delegate { };
    public event EventHandler<BackToFrontNotifyEventArgs> OnFatal = delegate { };
    #endregion

    #region Invocation
    public void SendInfo(string title, string content = "", Exception? exception = null)
    {
        _logger.Verbose(exception, "Calling SendInfo with title {title} and content {content}", title, content);
        OnInfo.Invoke(this, CreateArgs(title, content, exception));
    }

    public void SendWarning(string title, string content = "", Exception? exception = null)
    {
        _logger.Verbose(exception, "Calling SendWarning with title {title} and content {content}", title, content);
        OnWarning.Invoke(this, CreateArgs(title, content, exception));
    }

    public void SendError(string title, string content = "", Exception? exception = null)
    {
        _logger.Verbose(exception, "Calling SendError with title {title} and content {content}", title, content);
        OnError.Invoke(this, CreateArgs(title, content, exception));
    }

    public void SendFatal(string title, string content = "", Exception? exception = null)
    {
        _logger.Verbose(exception, "Calling SendFatal with title {title} and content {content}", title, content);
        OnFatal.Invoke(this, CreateArgs(title, content, exception));
    }
    #endregion

    #region Util
    private static BackToFrontNotifyEventArgs CreateArgs(string title, string content, Exception? exception = null)
    {
        return new BackToFrontNotifyEventArgs(title, content, exception);
    }
    #endregion
}