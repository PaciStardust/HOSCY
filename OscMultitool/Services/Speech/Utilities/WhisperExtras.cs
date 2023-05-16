using Hoscy.Ui.Pages;
using System;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Whisper;

namespace Hoscy.Services.Speech.Utilities
{
    internal class TranscribeCallbacks : Callbacks
    {
        protected override void onNewSegment(Context sender, int countNew) //todo: [WHISPER] implement actual transcription, muting, differentiating between sounds and text
        {
            var results = sender.results(eResultFlags.Tokens);
            int segmentCount = results.segments.Length;
            int firstNewSegment = segmentCount - countNew;

            Logger.Debug($"Segments: {segmentCount}, New: {countNew}, FirstNew: {firstNewSegment}");

            for (int i = firstNewSegment; i < segmentCount; i++)
            {
                var segment = results.segments[i];
                var segmentText = segment.text?.ToString().Trim();

                Logger.Debug($"Segment {i}: {segmentText}");

                var tokens = results.getTokens(segment);
                
                for (int j = 0; j < tokens.Length; j++)
                {
                    Logger.Debug($"Token {j}: {tokens[i].text} {(tokens[i].hasFlag(eTokenFlags.Special) ? "N" : "S")}");
                }

                var speaker = sender.detectSpeaker(segment.time);
                Logger.Debug("Speaker: " + speaker.ToString());

                //if (segmentText != "[BLANK_AUDIO]")
                //{
                //    PageInfo.SetMessage(segmentText, false, false);
                //}
            }
        }
    }

    internal class CaptureThread : CaptureCallbacks
    {
        private readonly TranscribeCallbacks _callbacks;
        private readonly Thread _thread;
        private readonly Context _context;
        private readonly iAudioCapture _capture;

        #region Startup
        internal CaptureThread(Context ctx, iAudioCapture capture)
        {
            _callbacks = new();
            _context = ctx;
            _capture = capture;

            _thread = new(ThreadRunCapture)
            {
                Name = "Whisper Capture Thread",
                IsBackground = true
            };
            _thread.Start();
        }

        private ExceptionDispatchInfo? _edi;
        private void ThreadRunCapture()
        {
            try
            {
                _context.runCapture(_capture, _callbacks, this);
            }
            catch (Exception ex)
            {
                _edi = ExceptionDispatchInfo.Capture(ex);
            }
        }

        /// <summary>
        /// Joins the thread, required for detecting errors
        /// </summary>
        internal void Join()
        {
            _thread.Join();
            _edi?.Throw();
        }

        #endregion

        #region Stopping
        private volatile bool _shouldQuit = false;

        /// <summary>
        /// Stops the CaptureThread
        /// </summary>
        internal void Stop() //todo: [WHISPER] ensure mute on stop?
            => _shouldQuit = true;
        protected override bool shouldCancel(Context sender) =>
            _shouldQuit;
        #endregion

        #region Other
        //todo: [WHISPER] is this needed?
        protected override void captureStatusChanged(Context sender, eCaptureStatus status)
        {
            Logger.Debug($"CaptureStatusChanged: {status}");
        }
        #endregion
    }
}
