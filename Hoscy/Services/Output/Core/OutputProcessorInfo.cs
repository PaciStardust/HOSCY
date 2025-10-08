namespace Hoscy.Services.Output.Core;

/// <summary>
/// Contains the information for an OutputProcessor
/// </summary>
public record OutputProcessorInfo
{
    public required string Name;
    public required string Description;
    public required OutputProcessorInfoFlags Flags;
}