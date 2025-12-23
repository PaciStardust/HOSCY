namespace HoscyCore.Services.Output.Core;

/// <summary>
/// Flags to determine settings to be displayed in UI
/// </summary>
[Flags]
public enum OutputProcessorInfoFlags
{
    SupportsMessages = 1,
    SupportsNotifications = 2,
    SupportsProcessingIndicator = 4,
}