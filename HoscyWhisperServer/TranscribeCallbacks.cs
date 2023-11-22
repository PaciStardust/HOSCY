using System.Collections.Generic;
using Whisper;

namespace HoscyWhisperServer
{
    internal class TranscribeCallbacks : Callbacks
    {
        #region Startup
        internal bool HasStarted { get; private set; } = false;
        //This is an insanely jank way to detect proper startup lmfao
        protected override bool onEncoderBegin(Context sender)
        {
            HasStarted = true;
            return true;
        }
        #endregion

        #region Recognition
        internal List<sSegment> Segments = new();

        protected override void onNewSegment(Context sender, int countNew)
        {
            var results = sender.results(eResultFlags.Timestamps);

            int segmentCount = results.segments.Length;
            int firstNewSegment = segmentCount - countNew;

            for (int i = firstNewSegment; i < segmentCount; i++)
                Segments.Add(results.segments[i]);
        }
        #endregion
    }
}