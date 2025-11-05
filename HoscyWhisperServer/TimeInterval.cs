namespace HoscyWhisperServer
{
    internal readonly struct TimeInterval
    {
        internal readonly DateTimeOffset Start { get; init; } = DateTimeOffset.MinValue;
        internal readonly DateTimeOffset End { get; init; } = DateTimeOffset.MaxValue;

        internal TimeInterval(DateTimeOffset start, DateTimeOffset end)
        {
            Start = start;
            End = end;
        }
    }
}
