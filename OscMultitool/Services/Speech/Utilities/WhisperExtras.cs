using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Whisper;

namespace Hoscy.Services.Speech.Utilities
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
            {
                var segment = results.segments[i];
                var segmentText = segment.text?.ToString().Trim();

                if (string.IsNullOrWhiteSpace(segmentText))
                    continue;

                Logger.Debug($"{i}: {segmentText}");
                Segments.Add(segment);
            }
        }
        #endregion
    }

    internal class CaptureThread : CaptureCallbacks
    {
        private readonly TranscribeCallbacks _callbacks;
        private readonly Thread _thread;
        private readonly Context _context;
        private readonly iAudioCapture _capture;
        internal ExceptionDispatchInfo? StartException { get; private set; }
        internal DateTime StartTime { get; init; }

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
            StartTime = DateTime.Now;

            var loopCounter = 0;
            while (!_callbacks.HasStarted)
            {
                if (++loopCounter > 6000) //60 seconds have passed
                {
                    var ex = new ApplicationException("Whisper startup taken over 60 seconds, cancelling");
                    StartException = ExceptionDispatchInfo.Capture(ex);
                    break;
                }
                Thread.Sleep(10);
            }
        }

        private void ThreadRunCapture()
        {
            try
            {
                _context.runCapture(_capture, _callbacks, this);
            }
            catch (Exception ex)
            {
                StartException = ExceptionDispatchInfo.Capture(ex);
            }
        }
        #endregion

        #region Stopping
        private volatile bool _shouldQuit = false;

        /// <summary>
        /// Stops the CaptureThread
        /// </summary>
        internal void Stop()
            => _shouldQuit = true;
        protected override bool shouldCancel(Context sender) =>
            _shouldQuit;
        #endregion

        #region Event
        private bool _lastTranscribing = false;
        protected override void captureStatusChanged(Context sender, eCaptureStatus status)
        {
            Logger.Debug($"CaptureStatusChanged: {status}");
            if ((eCaptureStatus.Transcribing & status) != 0)
            {
                _lastTranscribing = true;
                return;
            }
                
            if (_lastTranscribing)
            {
                _lastTranscribing = false;

                var seg = _callbacks.Segments;
                if (seg.Count == 0)
                    return;

                HandleSpeechRecognized(seg.ToArray());
                seg.Clear();
            }
        }

        internal event EventHandler<sSegment[]> SpeechRecognized = delegate { };

        private void HandleSpeechRecognized(sSegment[] e)
            => SpeechRecognized.Invoke(null, e);
        #endregion
    }
}