using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace HoscyCore.Services.Recognition.Modules;

[PrototypeLoadIntoDiContainer(typeof(WhisperRecognitionModuleStartInfo), Lifetime.Singleton)]
public class WhisperRecognitionModuleStartInfo : IRecognitionModuleStartInfo
{
    public string Name => "Whisper AI Recognizer";
    public string Description => "Local AI, quality / RAM, VRAM usage varies, startup may take a while";
    public Type ModuleType => typeof(WhisperRecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Microphone | RecognitionModuleConfigFlags.Whisper;
}

[PrototypeLoadIntoDiContainer(typeof(WhisperRecognitionModule), Lifetime.Transient)]
public class WhisperRecognitionModule
(
    ILogger logger,
    ConfigModel config,
    IRecognitionModelProviderService modelProvider
)
    : RecognitionModuleBase(logger.ForContext<WhisperRecognitionModule>())
{
    #region Vars
    private readonly ConfigModel _config = config;
    private readonly IRecognitionModelProviderService _modelProvider = modelProvider;

    private readonly List<RecognitionTimeInterval> _muteTimes = [];
    private readonly Dictionary<string, int> _filteredActions = [];

    private Process? _whisperProcess = null;
    private DateTimeOffset? _timeStarted = null;
    #endregion

    #region Start / Stop
    protected override bool IsStarted()
        => _whisperProcess is not null || _timeStarted is not null;

    protected override bool IsProcessing()
        => _whisperProcess is not null && _timeStarted is not null && _whisperProcess.Id != int.MinValue
            && !_whisperProcess.HasExited && _timeStarted > DateTimeOffset.MinValue;

    protected override void StartForService()
    {
        _timeStarted = null;

        _muteTimes.Clear();
        _muteTimes.Add(new(DateTimeOffset.MinValue, DateTimeOffset.MaxValue)); //Default value - indefinite mute

        _logger.Debug("Starting up whisper process");
        var process = CreateProcess();
        try
        {
            process.OutputDataReceived += ProcessOutputRecieved;
            process.Start();
            process.BeginOutputReadLine();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Encountered exception starting process, cleaning up process before stopping");
            CleanupProcess(process);
            throw;
        }

        while(_timeStarted is null) 
        {
            if (process.HasExited)
            {
                process.Dispose();
                _logger.Warning("Whisper process has exited before startup");
                throw new StartStopServiceException("Whisper process has exited before startup");
            }
            Thread.Sleep(10);
        }
        if (_timeStarted == DateTime.MinValue)
        {
            _logger.Warning("Process has finished starting, but failed to parse start time");
            CleanupProcess(process);
            throw new StartStopServiceException("Process has finished starting, but failed to parse start time");
        }

        process.Exited += OnProcessExited;
        _whisperProcess = process;
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override void StopForRecognitionModule()
    {
        if (_config.Recognition_Whisper_Dbg_LogFilteredNoises && _filteredActions.Count > 0)
            LogFilteredActions();

        if (_whisperProcess != null)
        {
            _whisperProcess.Exited -= OnProcessExited;
            CleanupProcess(_whisperProcess);
            _whisperProcess = null;
        }
    }
    #endregion

    #region Listen Control
    public override bool IsListening => 
        _muteTimes.Count == 0 || _muteTimes[^1].End < DateTime.Now;

    protected override bool SetListeningForRecognitionModule(bool state)
    {
        if (state)
        {
            //Set mute records last time to not be indefinite
            if (_muteTimes.Count > 0)
                _muteTimes[^1] = new(_muteTimes[^1].Start, DateTimeOffset.UtcNow);
        }
        else
            //Add new indefinite mute record
            _muteTimes.Add(new(DateTimeOffset.UtcNow, DateTimeOffset.MaxValue));

        return IsListening;
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion

    #region Process Control
    private Process CreateProcess()
    {
        var path = Path.Combine(PathUtils.PathExecutableFolder, "HoscyWhisperServer.exe");
        if (!File.Exists(path))
        {
            path = Path.Combine(PathUtils.PathExecutableFolder, "HoscyWhisperServer");
        }

        return new()
        {
            StartInfo = new ProcessStartInfo(path)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ErrorDialog = false,
                CreateNoWindow = true,
                Arguments = GenerateProcessArguments()
            },
            EnableRaisingEvents = true
        };
    }

    private string GenerateProcessArguments()
    {
        _config.Recognition_Whisper_Models.TryGetValue(_config.Recognition_Whisper_SelectedModel, out var path);

        var dict = new Dictionary<string, object>()
        {
            { "ModelPath", path ?? string.Empty },
            { "GraphicsAdapter", GetGraphicsAdapter() ?? string.Empty },
            { "WhisperThreads", _config.Recognition_Whisper_Cfg_ThreadsUsed },
            { "WhisperSingleSegment", _config.Recognition_Whisper_Cfg_UseSingleSegmentMode },
            { "MicId", _config.Audio_CurrentMicrophoneName }, //todo: this might need fixing
            { "WhisperToEnglish", _config.Recognition_Whisper_Cfg_TranslateToEnglish },
            { "WhisperMaxContext", _config.Recognition_Whisper_Cfg_MaxContext },
            { "WhisperMaxSegLen", _config.Recognition_Whisper_Cfg_MaxSegmentLength },
            { "WhisperLanguage", _config.Recognition_Whisper_Cfg_Language },
            { "WhisperRecMaxDuration", _config.Recognition_Whisper_Cfg_MaxSentenceDurationSeconds },
            { "WhisperRecPauseDuration", _config.Recognition_Whisper_Cfg_DetectPauseDurationSeconds },
            { "WhisperHighPerformance", _config.Recognition_Whisper_Cfg_IncreaseThreadPriority },
            { "ParentPid", Environment.ProcessId }
        };

        return JsonConvert.SerializeObject(dict, Formatting.None).Replace("\"", "'");
    }

    private void CleanupProcess(Process? process)
    {
        if (process is null) return;
        try
        {
            process.Kill();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Process could not be stopped, this might also be the case when no process is available");
        }
        process.Dispose();
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (_whisperProcess is null)
        {
            _logger.Warning("Whisper process has exited for unknown reasons, stopping recognizer");
            return;
        }
        else
        {
            var errorText = _whisperProcess.StandardError.ReadToEnd();
            if (string.IsNullOrWhiteSpace(errorText))
            {
                _logger.Warning("Whisper process has exited with code {exitCode}, stopping recognizer",
                    _whisperProcess.ExitCode);
            }
            else
            {
                _logger.Warning("Recognizer stopped due to whisper process exiting with code {exitCode}:\n\n{errorText}",
                    _whisperProcess.ExitCode, errorText);
            }
        }
        Stop(); //todo: does this actually work or should something be invoked instead?
    }
    #endregion

    #region Process Output Handling
    private const string SEPERATOR = "|||";
    private static readonly IReadOnlyDictionary<string, LogEventLevel> _toServerity = new Dictionary<string, LogEventLevel>()
    {
        { "Info", LogEventLevel.Information },
        { "Error", LogEventLevel.Error },
        { "Warning", LogEventLevel.Warning },
        { "Debug", LogEventLevel.Debug }
    };

    private void ProcessOutputRecieved(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null) return;

        var indexOfSeparator = e.Data.IndexOf(SEPERATOR);
        if (indexOfSeparator == -1)
        {
            _logger.Debug("Data without separator: " + e.Data);
            return;
        }

        var flag = e.Data.AsSpan()[..indexOfSeparator];
        var text = e.Data[(indexOfSeparator + SEPERATOR.Length)..];

        if (SpanCompare(flag, "Segments"))
        {
            HandleSegmentOutput(text);
            return;
        }
        if (SpanCompare(flag, "Speech"))
        {
            InvokeSpeechActivity(text == "T");
            return;
        }

        if (HandleIfLogOutput(flag, text.AsSpan()))
            return;

        if (SpanCompare(flag, "Loaded"))
        {
            if(DateTimeOffset.TryParse(text, out var started))
                _timeStarted = started;
            else
                _timeStarted = DateTimeOffset.MinValue;
            return;
        }

        _logger.Debug("Unknown data: " + e.Data);
    }

    private void HandleSegmentOutput(string text)
    {
        try
        {
            var bytes = Convert.FromBase64String(text);
            var decoded = Encoding.UTF8.GetString(bytes);
            var segments = JsonConvert.DeserializeObject<(string, TimeSpan, TimeSpan)[]>(decoded);
            HandleSegments(segments);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to decode base64 {text}", text);
            SetFault(ex);
        }
    }

    private bool HandleIfLogOutput(ReadOnlySpan<char> flag, ReadOnlySpan<char> text)
    {
        LogEventLevel? severity = null;
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
            _logger.Write(severity.Value, text.ToString().Replace("[NL]", "\n"));
            return true;
        }

        return false;
    }
    #endregion

    #region Segment Handling
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
            InvokeSpeechRecognized(joined);
    }

    private bool IsSpokenWhileMuted(DateTimeOffset startTime, (string, TimeSpan, TimeSpan) segment)
    {
        var start = startTime + segment.Item2;
        var end = startTime + segment.Item3;

        //Remove all unneeded values
        _muteTimes.RemoveAll(x => x.End <= start);

        if (_muteTimes.Count != 0)
        {
            var first = _muteTimes.First();
            if (end > first.Start || start < first.End)
                return true;
        }
        return false;
    }
    #endregion

    #region Speech Segment Cleanup
    private static readonly Regex _actionDetector = new(@"( *)[\[\(\*] *([^\]\*\)]+) *[\*\)\]]");
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
            foreach (var filter in _config.Recognition_Whisper_Cfg_NoiseFilter.Values)
            {
                if (groupText.StartsWith(filter))
                {
                    valid = true;
                    break;
                }
            }

            if (valid)
            {
                sb.Insert(match.Index, $"{match.Groups[1].Value}|{groupText}|");
            }
            else if (_config.Recognition_Whisper_Dbg_LogFilteredNoises && groupText != "BLANK_AUDIO")
            {
                //Adding it to the filtered list
                if (_filteredActions.TryGetValue(groupText, out var key))
                    _filteredActions[groupText] = key + 1;
                else
                    _filteredActions[groupText] = 1;
                _logger.Debug("Noise \"{groupText}\" filtered out by whisper noise whitelist", groupText);
            }
        }

        return sb.ToString();
    }

    private string CleanText(string text)
    {
        text = _config.Recognition_Whisper_Fix_RemoveRandomBrackets
            ? text.TrimStart(' ', '-', '(', '[', '*').TrimEnd()
            : text.TrimStart(' ', '-').TrimEnd();

        return text.Replace('|', '*');
    }

    private void LogFilteredActions() //todo: ???
    {
        var sortedActions = _filteredActions.Select(x => (x.Key, x.Value))
            .OrderByDescending(x => x.Value)
            .Select(x => $"\"{x.Key}\" ({x.Value}x)");
        _logger.Information("Filtered actions by Whisper: {filterActions}", string.Join(", ", sortedActions));
    }
    #endregion

    #region Utils
    private string? GetGraphicsAdapter()
    {
        if (!string.IsNullOrWhiteSpace(_config.Recognition_Whisper_Cfg_GraphicsAdapter))
            return _config.Recognition_Whisper_Cfg_GraphicsAdapter;

        var graphicAdapters = _modelProvider.GetGraphicsAdapters();
        if (graphicAdapters.Count != 0)
            return graphicAdapters[0];

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