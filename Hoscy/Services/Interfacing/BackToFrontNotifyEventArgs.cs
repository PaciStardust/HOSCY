using System;

namespace Hoscy.Services.Interfacing;

/// <summary>
/// Represents arguments passed from backend to frontend for notifications
/// </summary>
public class BackToFrontNotifyEventArgs(string title, string content, Exception? exception = null) : EventArgs
{
    public string Title { get; init; } = title;
    public string Content { get; init; } = content;
    public Exception? Exception { get; init; } = exception;
}