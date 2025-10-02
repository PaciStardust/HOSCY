using System;

namespace Hoscy.Services.Interfacing;

public interface IBackToFrontNotifyService
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

