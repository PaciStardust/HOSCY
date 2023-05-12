using Hoscy.Services.Speech.Utilities;
using System;
using Whisper;

namespace Hoscy.Services.Speech.Recognizers
{
    internal class RecognizerWhisper : RecognizerBase //todo: [WHISPER] test
    {
        private CaptureThread? _cptThread;

        new internal static RecognizerPerms Perms => new()
        {
            Description = "Local AI, quality / RAM usage varies, startup may take a while",
            UsesMicrophone = true,
            UsesWhisperModel = true
        };

        internal override bool IsListening => _cptThread?.GetListeningStatus() ?? false;

        #region Start / Stop and Muting
        protected override bool StartInternal()
        {
            var model = Library.loadModel(Config.Speech.WhisperModels[Config.Speech.WhisperModelCurrent]); //todo: [WHISPER] error handling
            //todo: [WHISPER] options

            var captureDevice = GetCaptureDevice();
            if (captureDevice == null)
                return false;

            var ctx = model.createContext();
            ApplyParameters(ref ctx.parameters);

            CaptureThread thread = new(ctx, captureDevice);

            var error = thread.GetError();
            if (error != null)
            {
                Logger.Error(error.SourceException);
                return false;
            }
            _cptThread = thread;

            return true;
        }

        protected override void StopInternal()
        {
            Textbox.EnableTyping(false);
            _cptThread?.Stop();
            _cptThread = null;
        }

        protected override bool SetListeningInternal(bool enabled)
            => _cptThread?.SetListening(enabled) ?? false;
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
                dropStartSilence = 0.25f,
                minDuration = 1.0f,
                maxDuration = 8f,
                pauseDuration = 1.0f
            };

            return medf.openCaptureDevice(deviceId.Value,cp);
        }

        private int n_threads = Environment.ProcessorCount;
        private int offset_t_ms = 0;
        private int duration_ms = 0;
        private int max_context = 0;
        private int max_len = 0;

        private float word_thold = 0.01f;

        private bool speed_up = false;
        private bool translate = false;
        private bool print_special = true;
        private bool print_progress = true;
        private bool no_timestamps = false;

        private eLanguage language = eLanguage.English;

        const bool output_wts = false;

        private void ApplyParameters(ref Parameters p)
        {
            p.setFlag(eFullParamsFlags.PrintRealtime, true);
            p.setFlag(eFullParamsFlags.PrintProgress, print_progress);
            p.setFlag(eFullParamsFlags.PrintTimestamps, !no_timestamps);
            p.setFlag(eFullParamsFlags.PrintSpecial, print_special);
            p.setFlag(eFullParamsFlags.Translate, translate);
            p.language = language;
            p.cpuThreads = n_threads;
            if (max_context >= 0)
                p.n_max_text_ctx = max_context;
            p.offset_ms = offset_t_ms;
            p.duration_ms = duration_ms;
            p.setFlag(eFullParamsFlags.TokenTimestamps, output_wts || max_len > 0);
            p.thold_pt = word_thold;
            p.max_len = output_wts && max_len == 0 ? 60 : max_len;
            p.setFlag(eFullParamsFlags.SpeedupAudio, speed_up);
        }
        #endregion
    }
}
