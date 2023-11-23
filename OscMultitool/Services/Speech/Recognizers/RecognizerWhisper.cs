using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Hoscy.Services.Speech.Utilities;

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

        private readonly List<TimeInterval> _muteTimes = new()
        {
            new(DateTime.MinValue, DateTime.MaxValue) //Default value - indefinite mute
        };
        private readonly Dictionary<string, int> _filteredActions = new();

        private Process? _whisperProcess;
        private bool _hasLoaded = false;
        private DateTime? _timeStarted;

        #region Start / Stop
        protected override bool StartInternal()
        {
            Logger.Info("Starting Whisper Process");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo(Path.Combine(Utils.PathExecutableFolder, "HoscyWhisperServer.exe"))
                {
                    RedirectStandardOutput = true,
                    ErrorDialog = false,
                    CreateNoWindow = true,
                    Arguments = GenerateArguments()
                },
                EnableRaisingEvents = true
            };

            try
            {
                process.OutputDataReceived += ProcessOutputRecieved;
                process.Start();
                process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                process.Kill();
                process.Dispose();
                return false;
            }

            while(!_hasLoaded) 
            {
                if (process.HasExited)
                    return false;
                Thread.Sleep(10);
            }
            if (_timeStarted is null) //todo: redo all logic associated with "Loaded"
                return false;

            process.Exited += ProcessExited;
            _whisperProcess = process;
            return true;
        }

        private void ProcessExited(object? sender, EventArgs e)
        {
            Logger.Debug("Whisper process has exited, stopping recognizer");
            StopInternal();
        }

        protected override void StopInternal()
        {
            HandleSpeechActivityUpdated(false);
            if (Config.Speech.WhisperLogFilteredNoises && _filteredActions.Count > 0)
                LogFilteredActions();

            if (_whisperProcess != null)
            {
                _whisperProcess.Exited -= ProcessExited;
                _whisperProcess.Kill();
                _whisperProcess.Dispose();
                _whisperProcess = null;
            }
        }

        private static string GenerateArguments()
        {
            Config.Speech.WhisperModels.TryGetValue(Config.Speech.WhisperModelCurrent, out var path);

            var dict = new Dictionary<string, object>()
            {
                { "ModelPath", path ?? string.Empty },
                { "GraphicsAdapter", GetGraphicsAdapter() ?? string.Empty },
                { "WhisperThreads", Config.Speech.WhisperThreads },
                { "WhisperSingleSegment", Config.Speech.WhisperSingleSegment },
                { "MicId", Config.Speech.MicId },
                { "WhisperToEnglish", Config.Speech.WhisperToEnglish },
                { "WhisperMaxContext", Config.Speech.WhisperMaxContext },
                { "WhisperMaxSegLen", Config.Speech.WhisperMaxSegLen },
                { "WhisperLanguage", Config.Speech.WhisperLanguage },
                { "WhisperRecMaxDuration", Config.Speech.WhisperRecMaxDuration },
                { "WhisperRecPauseDuration", Config.Speech.WhisperRecPauseDuration },
                { "WhisperHighPerformance", Config.Speech.WhisperHighPerformance },
                { "ParentPid", Environment.ProcessId }
            };

            return JsonConvert.SerializeObject(dict, Formatting.None).Replace("\"", "'");
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
        #endregion

        #region Input Processing
        private static readonly string _separator = "|||";
        private static readonly IReadOnlyDictionary<string, LogSeverity> _toServerity = new Dictionary<string, LogSeverity>()
        {
            { "Info", LogSeverity.Info },
            { "Error", LogSeverity.Error },
            { "Warning", LogSeverity.Info },
            { "Debug", LogSeverity.Debug }
        };

        private void ProcessOutputRecieved(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null) return;

            var indexOfSeparator = e.Data.IndexOf(_separator);
            if (indexOfSeparator != -1)
            {
                var flag = e.Data.AsSpan()[..indexOfSeparator];
                var text = e.Data.AsSpan()[(indexOfSeparator + _separator.Length)..];

                if (SpanCompare(flag, "Segments"))
                {
                    var segments = JsonConvert.DeserializeObject<(string, TimeSpan, TimeSpan)[]>(text.ToString());
                    HandleSegments(segments);
                    return;
                }
                if (SpanCompare(flag, "Speech"))
                {
                    HandleSpeechActivityUpdated(SpanCompare(text, "T"));
                    return;
                }
                if (SpanCompare(flag, "Loaded"))
                {
                    var success = DateTime.TryParse(text.ToString(), out var started);
                    if (success)
                        _timeStarted = started;
                    Logger.Debug("")
                    _hasLoaded = true;
                    return;
                }

                LogSeverity? severity = null;
                foreach (var kvp in _toServerity)
                {
                    if (SpanCompare(flag, kvp.Key))
                    {
                        severity = kvp.Value;
                        break;
                    }
                }
                if (severity is not null)
                {
                    Logger.Log(text.ToString().Replace("[NL]", "\n"), severity.Value);
                    return;
                }
            }

            Logger.Debug("Unknown data: " + e.Data);
        }

        private void HandleSegments((string, TimeSpan, TimeSpan)[]? segments)
        {
            if (segments == null || segments.Length == 0) return;

            //Ensure segments are ordered correctly
            var sortedSegments = segments.OrderBy(x => x.Item2);
            var strings = new List<string>();

            foreach (var segment in sortedSegments)
            {
                if (string.IsNullOrWhiteSpace(segment.Item1) || IsSpokenWhileMuted(_timeStarted!.Value, segment))
                    continue;

                var fixedActionText = ReplaceActions(segment.Item1);
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
        private bool IsSpokenWhileMuted(DateTime startTime, (string, TimeSpan, TimeSpan) segment)
        {
            var start = startTime + segment.Item2;
            var end = startTime + segment.Item3;

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
        #endregion

        #region Text Processing
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

        #region Utils
        private static string? GetGraphicsAdapter()
        {
            if (!string.IsNullOrWhiteSpace(Config.Speech.WhisperGraphicsAdapter))
                return Config.Speech.WhisperGraphicsAdapter;

            if (Devices.GraphicsAdapters.Any())
                return Devices.GraphicsAdapters[0];

            return null;
        }

        private static bool SpanCompare(ReadOnlySpan<char> span, string text)
        {
            if (span.Length != text.Length)
                return false;

            for (int i = 0; i < span.Length; i++) 
                if (span[i] != text[i]) return false;

            return true;
        }
        #endregion
    }
}
