namespace HoscyWhisperV2Process;

public record RecognitionQueueItem
{
    public required uint Id { get; init; }
    public required uint SubId { get; init; }
    public required bool IsFinal { get; init; }
    public required byte[] AudioData { get; init; }
}