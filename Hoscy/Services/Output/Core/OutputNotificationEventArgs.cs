using System;

namespace Hoscy.Services.Output.Core;

public class OutputNotificationEventArgs(string contents, OutputNotificationPriority priority) : EventArgs
{
    public string Contents { get; init; } = contents;
    public OutputNotificationPriority Priority { get; init; } = priority;
}