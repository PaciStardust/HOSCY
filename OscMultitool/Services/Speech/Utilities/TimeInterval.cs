using System;

namespace Hoscy.Services.Speech.Utilities
{
    internal readonly struct TimeInterval
    {
        internal readonly DateTime Start { get; init; } = DateTime.MinValue;
        internal readonly DateTime End { get; init; } = DateTime.MaxValue;

        internal TimeInterval(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
    }
}
