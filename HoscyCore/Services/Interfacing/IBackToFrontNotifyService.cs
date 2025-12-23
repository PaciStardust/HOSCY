using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Interfacing;

/// <summary>
/// Service to send Information from the backend services to the frontend to display
/// </summary>
public interface IBackToFrontNotifyService : IService
{
    public event EventHandler<BackToFrontNotifyEventArgs> OnInfo;
    public event EventHandler<BackToFrontNotifyEventArgs> OnWarning;
    public event EventHandler<BackToFrontNotifyEventArgs> OnError;
    public event EventHandler<BackToFrontNotifyEventArgs> OnFatal;

    public void SendInfo(string title, string content = "", Exception? exception = null);
    public void SendWarning(string title, string content = "", Exception? exception = null);
    public void SendError(string title, string content = "", Exception? exception = null);
    public void SendFatal(string title, string content = "", Exception? exception = null);
}

