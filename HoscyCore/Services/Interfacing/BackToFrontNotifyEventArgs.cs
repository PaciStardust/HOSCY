namespace HoscyCore.Services.Interfacing;

/// <summary>
/// Represents arguments passed from backend to frontend for notifications
/// </summary>
public class BackToFrontNotifyEventArgs(BackToFrontNotifyLevel level, string title, string content, Exception? exception = null) : EventArgs
{
    public BackToFrontNotifyLevel Level { get; init; } =  level;
    public string Title { get; init; } = title;
    public string Content { get; init; } = content;
    public Exception? Exception { get; init; } = exception;
}