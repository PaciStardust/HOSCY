using Hoscy.Services.Speech.Utilities;
using Hoscy.Services.Speech.Utilities.Whisper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Whisper;

namespace Hoscy.Services.Speech.Recognizers
{
    internal class RecognizerWhisper : RecognizerBase
    {
        new internal static RecognizerPerms Perms => new()
        {
            Description = "Local AI, quality / RAM, VRAM usage varies, startup may take a while",
            UsesMicrophone = true,
            Type = RecognizerType.Whisper
        };

        internal override bool IsListening => _muteTimes.Count == 0 || _muteTimes[^1].End < DateTime.Now;

        private CaptureThread? _cptThread;
        private readonly List<TimeInterval> _muteTimes = new()
        {
            new(DateTime.MinValue, DateTime.MaxValue) //Default value - indefinite mute
        };
        private readonly Dictionary<string, int> _filteredActions = new();

        #region Starting / Stopping
        protected override bool StartInternal()
        {
            try
            {
                Logger.Info("Attempting to load whisper model");
                var valid = Config.Speech.WhisperModels.TryGetValue(Config.Speech.WhisperModelCurrent, out var path);
                if (!valid || !File.Exists(path))
                {
                    Logger.Error("A Whisper AI model has not been picked or it's path is invalid.\n\nTo use Whisper speech recognition please provide an AI model. Information can be found in the quickstart guide on GitHub\n\nIf you do not want to use Whisper, please change the recognizer type on the speech page");
                    return false;
                }

                //var model = Library.loadModel(path, impl: Config.Speech.WhisperCpuOnly ? eModelImplementation.Reference : eModelImplementation.GPU); Disabled due to library issues
                var adapter = GetGraphicsAdapter();
                Logger.Debug($"Using Graphics Adapter {adapter ?? "NULL"} for Whisper recognition");
                var model = Library.loadModel(path, adapter: adapter);

                var captureDevice = GetCaptureDevice();
                if (captureDevice == null)
                    return false;

                var ctx = model.createContext();
                ApplyParameters(ref ctx.parameters);

                Logger.Info("Starting whisper thread, this might take a while");
                CaptureThread thread = new(ctx, captureDevice);
                thread.StartException?.Throw();
                thread.SpeechRecognized += OnSpeechRecognized;
                thread.SpeechActivityUpdated += (s, o) => HandleSpeechActivityUpdated(o);
                _cptThread = thread;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start whisper recognizer");
                return false;
            }
            return true;
        }

        protected override void StopInternal()
        {
            HandleSpeechActivityUpdated(false);
            if (Config.Speech.WhisperLogFilteredNoises && _filteredActions.Count > 0)
                LogFilteredActions();
            _cptThread?.Stop();
            _cptThread = null;
        }

        protected override bool SetListeningInternal(bool enabled)
        {
            if (IsListening == enabled)
                return false;

            if (enabled)
            {
                //Set mute records last time to not be indefinite
                if (_muteTimes.Count > 0)
                    _muteTimes[^1] = new(_muteTimes[^1].Start, DateTime.Now);
            }
            else
                //Add new indefinite mute record
                _muteTimes.Add(new(DateTime.Now, DateTime.MaxValue));

            return true;
        }

        /// <summary>
        /// Logs all filtered actions
        /// </summary>
        private void LogFilteredActions()
        {
            var sortedActions = _filteredActions.Select(x => (x.Key, x.Value))
                                                .OrderByDescending(x => x.Value)
                                                .Select(x => $"\"{x.Key}\" ({x.Value}x)");
            Logger.PInfo("Filtered actions by Whisper: " + string.Join(", ", sortedActions));
        }
        #endregion

        #region Transcription
        private void OnSpeechRecognized(object? sender, sSegment[] segments)
        {
            if (_cptThread == null || segments.Length == 0) return;

            //Ensure segments are ordered correctly
            var sortedSegments = segments.OrderBy(x => x.time.begin);
            var strings = new List<string>();

            foreach (var segment in sortedSegments)
            {
                if (string.IsNullOrWhiteSpace(segment.text) || IsSpokenWhileMuted(_cptThread.StartTime, segment))
                    continue;

                var fixedActionText = ReplaceActions(segment.text);
                fixedActionText = CleanText(fixedActionText);
                strings.Add(fixedActionText);
            }

            var joined = string.Join(' ', strings);
            if (!string.IsNullOrWhiteSpace(joined))
                HandleSpeechRecognized(joined);
        }

        /// <summary>
        /// Determines if a segment has been spoken while muted, also clears all unneeded values
        /// </summary>
        /// <param name="startTime">CaptureThread start time</param>
        /// <param name="segment">Segment to check</param>
        /// <returns>Was spoken while muted?</returns>
        private bool IsSpokenWhileMuted(DateTime startTime, sSegment segment)
        {
            var start = startTime + segment.time.begin;
            var end = startTime + segment.time.end;

            //Remove all unneeded values
            _muteTimes.RemoveAll(x => x.End <= start);

            if (_muteTimes.Any())
            {
                var first = _muteTimes.First();
                if (end > first.Start || start < first.End)
                    return true;
            }
            return false;
        }

        private static readonly Regex _actionDetector = new(@"( *)[\[\(\*] *([^\]\*\)]+) *[\*\)\]]");
        /// <summary>
        /// Replaces all actions if valid
        /// </summary>
        /// <param name="text">Text to replace actions in</param>
        /// <returns>Replaced text</returns>
        private string ReplaceActions(string text)
        {
            var matches = _actionDetector.Matches(text);
            if ((matches?.Count ?? 0) == 0)
                return text;

            var sb = new StringBuilder(text);
            //Reversed so we can use sb.Remove()
            foreach (var match in matches!.Reverse()) 
            {
                var groupText = match.Groups[2].Value.ToLower();
                sb.Remove(match.Index, match.Length);

                bool valid = false;
                foreach (var filter in Config.Speech.WhisperNoiseFilter.Values)
                {
                    if (groupText.StartsWith(filter))
                    {
                        valid = true;
                        break;
                    }
                }

                if (valid)
                {
                    if (Config.Speech.CapitalizeFirst)
                        groupText = groupText.FirstCharToUpper();

                    sb.Insert(match.Index, $"{match.Groups[1].Value}|{groupText}|");
                }
                else if (Config.Speech.WhisperLogFilteredNoises && groupText != "BLANK_AUDIO")
                {
                    //Adding it to the filtered list
                    if (_filteredActions.TryGetValue(groupText, out var key))
                        _filteredActions[groupText] = key + 1;
                    else
                        _filteredActions[groupText] = 1;
                    Logger.Log($"Noise \"{groupText}\" filtered out by whisper noise whitelist");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes odd AI noise and replaces action indicator with an asterisk
        /// </summary>
        /// <param name="text">Text to clean</param>
        private static string CleanText(string text)
        {
            text = Config.Speech.WhisperBracketFix
                ? text.TrimStart(' ', '-', '(', '[', '*').TrimEnd()
                : text.TrimStart(' ', '-').TrimEnd();

            return text.Replace('|', '*');
        }
        #endregion

        #region Setup
        private static iAudioCapture? GetCaptureDevice()
        {
            Logger.Info("Attempting to grab capture device for whisper");
            var medf = Library.initMediaFoundation();
            if (medf == null)
            {
                Logger.Error("No media foundation could be found");
                return null;
            }

            var devices = medf.listCaptureDevices();
            if (devices == null || devices.Length == 0)
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
                Logger.Warning("No matching audio device could be found, using default");
                deviceId = devices[0];
            }

            sCaptureParams cp = new()
            {
                dropStartSilence = 0.25f,
                minDuration = 1,
                maxDuration = Config.Speech.WhisperRecMaxDuration,
                pauseDuration = Config.Speech.WhisperRecPauseDuration
            };

            return medf.openCaptureDevice(deviceId.Value,cp);
        }

        private static void ApplyParameters(ref Parameters p)
        {
            //Threads
            var maxThreads = Environment.ProcessorCount;
            var cfgThreads = Config.Speech.WhisperThreads;

            if (cfgThreads < 0)
                p.cpuThreads = Utils.MinMax(maxThreads - cfgThreads, 1, maxThreads);
            else
                p.cpuThreads = cfgThreads > maxThreads || cfgThreads == 0 ? maxThreads : cfgThreads;

            //Normal Flags
            p.setFlag(eFullParamsFlags.SingleSegment, Config.Speech.WhisperSingleSegment);
            p.setFlag(eFullParamsFlags.Translate, Config.Speech.WhisperToEnglish);
            //p.setFlag(eFullParamsFlags.SpeedupAudio, Config.Speech.WhisperSpeedup); Disabled due to library issues

            //Number Flags
            if (Config.Speech.WhisperMaxContext >= 0)
                p.n_max_text_ctx = Config.Speech.WhisperMaxContext;
            p.setFlag(eFullParamsFlags.TokenTimestamps, Config.Speech.WhisperMaxSegLen > 0);
            p.max_len = Config.Speech.WhisperMaxSegLen;

            p.language = Config.Speech.WhisperLanguage;
            
            //Hardcoded
            p.thold_pt = 0.01f;
            p.duration_ms = 0;
            p.offset_ms = 0;
            p.setFlag(eFullParamsFlags.PrintRealtime, false);
            p.setFlag(eFullParamsFlags.PrintTimestamps, false);
        }

        private static string? GetGraphicsAdapter()
        {
            if (!string.IsNullOrWhiteSpace(Config.Speech.WhisperGraphicsAdapter))
                return Config.Speech.WhisperGraphicsAdapter;

            if (Devices.GraphicsAdapters.Any())
                return Devices.GraphicsAdapters[0];

            return null;
        }
        #endregion
    }
}