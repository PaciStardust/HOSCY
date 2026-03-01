namespace HoscyCore.Services.Recognition.Extra;

public record RecognitionTimeInterval
{
    public DateTimeOffset Start { get; init; } = DateTimeOffset.MinValue;
    public DateTimeOffset End { get; init; } = DateTimeOffset.MaxValue;

    public RecognitionTimeInterval(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }
}