using Hoscy.Services.Speech.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using Whisper;

namespace Hoscy.Services.Speech.Recognizers
{
    internal class RecognizerWhisper : RecognizerBase //todo: [WHISPER] test, cleanup
    {
        private CaptureThread? _cptThread;

        private DateTime _listenStart = DateTime.MinValue;
        private DateTime _muteStart = DateTime.MinValue;

        new internal static RecognizerPerms Perms => new()
        {
            Description = "Local AI, quality / RAM usage varies, startup may take a while",
            UsesMicrophone = true,
            Type = RecognizerType.Whisper
        };

        internal override bool IsListening => _listenStart > _muteStart;

        #region Start / Stop and Muting
        protected override bool StartInternal()
        {
            try
            {
                var model = Library.loadModel(Config.Speech.WhisperModels[Config.Speech.WhisperModelCurrent]);

                var captureDevice = GetCaptureDevice();
                if (captureDevice == null)
                    return false;

                var ctx = model.createContext();
                ApplyParameters(ref ctx.parameters);

                CaptureThread thread = new(ctx, captureDevice);
                thread.StartException?.Throw();
                thread.SpeechRecognized += OnSpeechRecognized;
                _cptThread = thread;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
            return true;
        }

        protected override void StopInternal()
        {
            Textbox.EnableTyping(false);
            _cptThread?.Stop();
            _cptThread = null;
        }

        protected override bool SetListeningInternal(bool enabled)
        {
            if (IsListening == enabled)
                return false;

            if (enabled)
                _listenStart = DateTime.Now;
            else
                _muteStart = DateTime.Now;

            return true;
        }
        #endregion

        #region Transcription

        private void OnSpeechRecognized(object? sender, sSegment[] segments)  //todo: [WHISPER] muting, differentiating between sounds and text
        {
            var strings = new List<string>();

            foreach (var segment in segments)
            {
                var start = _cptThread.StartTime + segment.time.begin;
                var end = _cptThread.StartTime + segment.time.begin;

                var text = segment.text ?? string.Empty;
                text = text.TrimStart(' ', '-').TrimEnd(' ');

                text = DetectAction(text) ?? text;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    text += $" {_cptThread.StartTime + segment.time.end} {DateTime.Now}";
                    strings.Add(text);
                }
            }

            //todo: null check
            ProcessMessage(string.Join(' ', strings));
        }

        private static readonly Regex _actionDetector = new(@"^[\[\(\*](.+)[\*\)\]]$");
        private static string? DetectAction(string text) //todo: [WHISPER] Filter Actions
        {
            var match = _actionDetector.Match(text);
            if (!match.Success)
                return null;

            var actionText = match.Groups[1].Value;
            return $"*{actionText.ToLower()}*";
        }
        #endregion

        #region Extra
        private static iAudioCapture? GetCaptureDevice()
        {
            var medf = Library.initMediaFoundation();
            if (medf == null)
            {
                Logger.Error("No media foundation could be found");
                return null;
            }

            var devices = medf.listCaptureDevices();
            if (devices == null)
            {
                Logger.Error("No audio devices could be found");
                return null;
            }

            CaptureDeviceId? deviceId = null;
            foreach (var device in devices)
            {
                if (device.displayName.StartsWith(Config.Speech.MicId))
                {
                    deviceId = device;
                    continue;
                }
            }

            if (deviceId == null)
            {
                Logger.Error("No matching audio device could be found");
                return null;
            }

            sCaptureParams cp = new()
            {
                dropStartSilence = Config.Speech.WhisperRecDropStartSilence,
                minDuration = Config.Speech.WhisperRecMinDuration,
                maxDuration = Config.Speech.WhisperRecMaxDuration,
                pauseDuration = Config.Speech.WhisperRecPauseDuration
            };

            return medf.openCaptureDevice(deviceId.Value,cp);
        }

        private eLanguage language = eLanguage.English; //todo: [WHISPER] Language setting

        private void ApplyParameters(ref Parameters p)
        {
            //Threads
            var maxThreads = Environment.ProcessorCount;
            var cfgThreads = Config.Speech.WhisperThreads;
            p.cpuThreads = cfgThreads > maxThreads || cfgThreads == 0 ? maxThreads : cfgThreads;

            //Normal Flags
            p.setFlag(eFullParamsFlags.SingleSegment, Config.Speech.WhisperSingleSegment);
            p.setFlag(eFullParamsFlags.Translate, Config.Speech.WhisperToEnglish);
            p.setFlag(eFullParamsFlags.SpeedupAudio, Config.Speech.WhisperSpeedup);

            //Number Flags
            if (Config.Speech.WhisperMaxContext >= 0)
                p.n_max_text_ctx = Config.Speech.WhisperMaxContext;
            p.setFlag(eFullParamsFlags.TokenTimestamps, Config.Speech.WhisperMaxSegLen > 0);
            p.max_len = Config.Speech.WhisperMaxSegLen;

            p.language = language;
            
            //Hardcoded
            p.thold_pt = 0.01f;
            p.duration_ms = 0;
            p.offset_ms = 0;
            p.setFlag(eFullParamsFlags.PrintRealtime, false);
            p.setFlag(eFullParamsFlags.PrintTimestamps, false);
        }
        #endregion
    }
}
