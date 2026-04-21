using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Output.Core;

public class OutputMessageLoop
(
    ILogger logger, 
    Func<string, OutputSettingsFlags, Task> handleMessage,
    Func<string, OutputNotificationPriority, OutputSettingsFlags, Task> handleNotification
) 
    : AsyncProcessingLoop<OutputMessage>(logger)
{
    private readonly Func<string, OutputSettingsFlags, Task> _handleMessage = handleMessage;
    private readonly Func<string, OutputNotificationPriority, OutputSettingsFlags, Task> _handleNotification = handleNotification;

    protected override void HandleClearedItem(OutputMessage item)
    {
        return;
    }

    protected override Task HandleItem(OutputMessage item)
    {
        return !item.Priority.HasValue
            ? _handleMessage(item.Contents, item.Settings)
            : _handleNotification(item.Contents, item.Priority.Value, item.Settings);
    }

    public bool AddMessage(string contents, OutputSettingsFlags settings)
        => Enqueue(new(contents, settings, null));

    public bool AddNotification(string contents, OutputNotificationPriority priority, OutputSettingsFlags settings)
        => Enqueue(new(contents, settings, priority));
}

public record OutputMessage(string Contents, OutputSettingsFlags Settings, OutputNotificationPriority? Priority);