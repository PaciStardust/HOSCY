namespace HoscyCore.Services.Output.Core;

/// <summary>
/// Contains the information for an OutputProcessor
/// </summary>
public record OutputProcessorInfo
{
    public required string Name;
    public required string Description;
    public required OutputProcessorInfoFlags Flags;
    public required Type ProcessorType;
}

/// <summary>
/// Flags to determine settings to be displayed in UI
/// </summary>
[Flags]
public enum OutputProcessorInfoFlags
{
    None = 0,
    OutputsAsText = 1,
    OutputsAsAudio = 2,
    OutputsAsOther = 4
}