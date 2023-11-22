using Newtonsoft.Json;
using Whisper;

namespace HoscyWhisperServer
{
    internal class Program
    {
        private static Dictionary<string, object> _config = new();
        private static CaptureThread? _cptThread;

        static void Main(string[] args)
        {
            var loadedConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(string.Join(' ', args));
            if (loadedConfig is null)
            {
                SendMessage(MessageType.Error, "Could not load config parameters into process");
                return;
            }
            _config = loadedConfig;

            try
            {
                var path = (string)_config["ModelPath"];
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    SendMessage(MessageType.Error, "A Whisper AI model has not been picked or it's path is invalid.\n\nTo use Whisper speech recognition please provide an AI model. Information can be found in the quickstart guide on GitHub\n\nIf you do not want to use Whisper, please change the recognizer type on the speech page");
                    return;
                }

                //var model = Library.loadModel(path, impl: Config.Speech.WhisperCpuOnly ? eModelImplementation.Reference : eModelImplementation.GPU); Disabled due to library issues
                var adapterString = (string)_config["GraphicsAdapter"];
                var adapter = string.IsNullOrWhiteSpace(adapterString) ? null : adapterString;
                SendMessage(MessageType.Info, $"Using Graphics Adapter {adapter ?? "NULL"} for Whisper recognition");
                var model = Library.loadModel(path, adapter: adapter);

                var captureDevice = GetCaptureDevice();
                if (captureDevice == null)
                {
                    SendMessage(MessageType.Error, "No capture device found");
                    return;
                }

                var ctx = model.createContext();
                ApplyParameters(ref ctx.parameters);

                SendMessage(MessageType.Info, "Starting whisper thread, this might take a while");
                CaptureThread thread = new(ctx, captureDevice, (bool)_config["WhisperHighPerformance"]);
                thread.StartException?.Throw();
                thread.SpeechRecognized += OnSpeechRecognized;
                thread.SpeechActivityUpdated += (s, o) => SendMessage(MessageType.Speech,o ? "T" : "F");
                _cptThread = thread;
            }
            catch (Exception ex)
            {
                SendMessage(MessageType.Error, "Failed to start whisper recognizer: " + ex.Message);
                return;
            }

            SendMessage(MessageType.Loaded, _cptThread.StartTime.ToString());
            Console.ReadKey();
        }

        private static void ApplyParameters(ref Parameters p)
        {
            //Threads
            var maxThreads = Environment.ProcessorCount;
            var cfgThreads = (int)_config["WhisperThreads"];

            if (cfgThreads < 0)
                p.cpuThreads = Math.Max(1, maxThreads - cfgThreads);
            else
                p.cpuThreads = cfgThreads > maxThreads || cfgThreads == 0 ? maxThreads : cfgThreads;

            //Normal Flags
            p.setFlag(eFullParamsFlags.SingleSegment, (bool)_config["WhisperSingleSegment"]);
            p.setFlag(eFullParamsFlags.Translate, (bool)_config["WhisperToEnglish"]);
            //p.setFlag(eFullParamsFlags.SpeedupAudio, Config.Speech.WhisperSpeedup); Disabled due to library issues

            //Number Flags
            if ((int)_config["WhisperMaxContext"] >= 0)
                p.n_max_text_ctx = (int)_config["WhisperMaxContext"];
            p.setFlag(eFullParamsFlags.TokenTimestamps, (int)_config["WhisperMaxSegLen"] > 0);
            p.max_len = (int)_config["WhisperMaxSegLen"];

            p.language = (eLanguage)_config["WhisperLanguage"];

            //Hardcoded
            p.thold_pt = 0.01f;
            p.duration_ms = 0;
            p.offset_ms = 0;
            p.setFlag(eFullParamsFlags.PrintRealtime, false);
            p.setFlag(eFullParamsFlags.PrintTimestamps, false);
        }

        private static iAudioCapture? GetCaptureDevice()
        {
            SendMessage(MessageType.Info, "Attempting to grab capture device for whisper");
            var medf = Library.initMediaFoundation();
            if (medf == null)
            {
                SendMessage(MessageType.Error, "No media foundation could be found");
                return null;
            }

            var devices = medf.listCaptureDevices();
            if (devices == null || devices.Length == 0)
            {
                SendMessage(MessageType.Error,"No audio devices could be found");
                return null;
            }

            CaptureDeviceId? deviceId = null;
            var micId = (string)_config["MicId"];
            foreach (var device in devices)
            {
                if (device.displayName.StartsWith(micId))
                {
                    deviceId = device;
                    continue;
                }
            }
            SendMessage(MessageType.Warning,"No matching audio device could be found, using default");
            deviceId ??= devices[0];

            sCaptureParams cp = new()
            {
                dropStartSilence = 0.25f,
                minDuration = 1,
                maxDuration = (float)_config["WhisperRecMaxDuration"],
                pauseDuration = (float)_config["WhisperRecPauseDuration"]
            };

            return medf.openCaptureDevice(deviceId.Value, cp);
        }

        private static void OnSpeechRecognized(object? sender, sSegment[] segments)
        {
            if (_cptThread == null || segments.Length == 0) return;

            //Ensure segments are ordered correctly
            var sortedSegments = segments.OrderBy(x => x.time.begin);

            SendMessage(MessageType.Segments, JsonConvert.SerializeObject(sortedSegments));
        }

        private static void SendMessage(MessageType type, string message)
            => Console.WriteLine($"{type}|||{message}");

        private enum MessageType
        {
            Segments,
            Speech,
            Warning,
            Error,
            Info,
            Loaded
        }
    }
}