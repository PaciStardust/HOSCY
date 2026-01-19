namespace HoscyCore.Services.Output.Core;

/// <summary>
/// Flags to determine settings to be displayed in UI
/// </summary>
[Flags]
public enum OutputProcessorInfoFlags //todo: these needed?
{
    SupportsMessages = 1,
    SupportsNotifications = 2,
    SupportsProcessingIndicator = 4,
    SupportsClearing = 8, //todo: ?
    OutputAsText = 16,
    OutputAsAudio = 32, //todo: ?
    OutputAsOther = 64
}

/*
TODO: Sketch

Some module wants to process text
-> Output Processor
-> Needs to deliver some info about "What do I allow" (preprocessing, output modes)
-> Filtering gets applied
-> Translation gets applied?
-> Gets sent to processor
*/