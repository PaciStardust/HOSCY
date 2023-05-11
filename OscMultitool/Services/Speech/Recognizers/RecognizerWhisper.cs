using Hoscy.Services.Speech.Utilities;
using NAudio.Wave;
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
            var model = Library.loadModel(Config.Speech.VoskModelCurrent); //todo: [WHISPER] error handling
            //todo: [WHISPER] options

            var captureDevice = GetCaptureDevice();
            if (captureDevice == null)
                return false;

            var ctx = model.createContext();
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
            _cptThread = null; //todo: [WHISPER] test if this null breaks anything?
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

            return medf.openCaptureDevice(deviceId.Value);
        }
        #endregion
    }
}
