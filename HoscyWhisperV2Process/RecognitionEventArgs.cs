using Whisper.net;

namespace HoscyWhisperV2Process;

public record RecognitionCallbackArgs
{
    public required uint Id { get; init; }
    public required uint SubId { get; init; }
    public required uint SegId { get; init; }
    public required SegmentData Data { get; init; }
}