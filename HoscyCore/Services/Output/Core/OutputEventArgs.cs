namespace HoscyCore.Services.Output.Core;

public class OutputMessageEventArgs(string contents, string[] outputs, string? translation) : EventArgs
{
    public string Contents { get; init; } = contents;
    public string[] Outputs { get; init; } = outputs;
    public string? Translation { get; init; } = translation;
}

public class OutputNotificationEventArgs(string contents, string[] outputs, OutputNotificationPriority priority) : EventArgs
{
    public string Contents { get; init; } = contents;
    public string[] Outputs { get; init; } = outputs;
    public OutputNotificationPriority Priority { get; init; } = priority;
}