using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Output.Core;

public class OutputMessageLoop
(
    ILogger logger, 
    Func<string, string, OutputSettingsFlags, Task> handleMessage,
    Func<string, string, OutputNotificationPriority, OutputSettingsFlags, Task> handleNotification
) 
    : AsyncProcessingLoop<OutputMessage>(logger)
{
    private readonly Func<string, string, OutputSettingsFlags, Task> _handleMessage = handleMessage;
    private readonly Func<string, string, OutputNotificationPriority, OutputSettingsFlags, Task> _handleNotification = handleNotification;

    protected override void HandleClearedItem(OutputMessage item)
    {
        return;
    }

    protected override Task HandleItem(OutputMessage item)
    {
        return !item.Priority.HasValue
            ? _handleMessage(item.Contents, item.Source ?? "Unknown", item.Settings)
            : _handleNotification(item.Contents, item.Source ?? "Unknown", item.Priority.Value, item.Settings);
    }

    public bool AddMessage(string contents, string? source, OutputSettingsFlags settings)
        => Enqueue(new(contents, source, settings, null));

    public bool AddNotification(string contents, string? source, OutputNotificationPriority priority, OutputSettingsFlags settings)
        => Enqueue(new(contents, source, settings, priority));
}

public record OutputMessage(string Contents, string? Source, OutputSettingsFlags Settings, OutputNotificationPriority? Priority);