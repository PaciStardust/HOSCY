using Hoscy.Ui.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Whisper;

namespace Hoscy.Services.Speech.Utilities
{
    internal class TranscribeCallbacks : Callbacks
    {
        private bool _isListening = false;
        internal bool GetListeningStatus() => _isListening;
        internal bool SetListening(bool enabled)
        {
            //todo: [WHISPER] implement
            return false;
        }

        private Regex _filterRegex = new(@"(?:\[.*\] )?(.*)\[.*\]");
        protected override void onNewSegment(Context sender, int countNew) //todo: [WHISPER] implement actual transcription, muting, differentiating between sounds and text
        {
            //todo: [WHISPER] pack in try
            TranscribeResult res = sender.results();
            ReadOnlySpan<sToken> tokens = res.tokens;
            var testing = res.segments.Length;
            int counter = 1;

            int s0 = res.segments.Length - countNew;
            if (s0 == 0)
                Debug.WriteLine("");
            string text = "";
            string stuff = "";
            for (int i = s0; i < res.segments.Length; i++)
            {
                sSegment seg = res.segments[i];

                stuff = seg.text.ToString().Trim();
                Logger.Debug($"segment {s0}: {stuff}");

                stuff = _filterRegex.Match(stuff).Groups[1].Value;
                if (stuff != "[BLANK_AUDIO]")
                {
                    PageInfo.SetMessage(stuff, false, false);
                }
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
                Logger.Error(ex); //todo: [WHISPER] Error does not get output otherwise
            }
        }

        internal ExceptionDispatchInfo? GetError()
            => _edi;
        #endregion

        #region Control
        private volatile bool _shouldQuit = false;

        internal void Stop() //todo: [WHISPER] ensure mute on stop?
            => _shouldQuit = true;
        protected override bool shouldCancel(Context sender) =>
            _shouldQuit;

        internal bool GetListeningStatus()
            => _callbacks.GetListeningStatus();
        internal bool SetListening(bool enabled)
            => _callbacks.SetListening(enabled);
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
