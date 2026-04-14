using System.Diagnostics;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Ipc;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Newtonsoft.Json;
using Serilog;
using HoscyCore.Services.Interfacing;
using System.Text;
using System.Text.RegularExpressions;

namespace HoscyCore.Services.Recognition.Modules;

[PrototypeLoadIntoDiContainer(typeof(WhisperRecognitionModuleStartInfo), Lifetime.Singleton)]
public class WhisperRecognitionModuleStartInfo : IRecognitionModuleStartInfo
{
    public string Name => "Whisper Recognizer";
    public string Description => "Local AI, quality / RAM, VRAM usage varies, startup may take a while";
    public Type ModuleType => typeof(WhisperRecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Microphone | RecognitionModuleConfigFlags.Whisper;
}

[PrototypeLoadIntoDiContainer(typeof(WhisperRecognitionModule), Lifetime.Transient)]
public class WhisperRecognitionModule(ILogger logger, ConfigModel config, IBackToFrontNotifyService notify)
    : RecognitionModuleBase(logger.ForContext<WhisperRecognitionModule>())
{
    #region Vars
    private readonly ConfigModel _config = config;
    private readonly IpcDataConverter _ipcConverter = new(logger.ForContext<WhisperRecognitionModule>());
    private readonly IBackToFrontNotifyService _notify = notify;

    private Process? _whisperProcess = null;
    private IpcSendPipe? _ipcPipe = null;
    private KeepAliveTimer? _keepAlive = null;
    #endregion

    #region Startup
    private bool _startedSignalReceived = false;
    protected override Res StartForService()
    {
        var pipeRes = ResC.TWrapR(() => new IpcSendPipe(_logger, _config.Debug_LogVerboseExtra),
            "Failed to create IPC pipe", _logger);
        if (!pipeRes.IsOk) return ResC.Fail(pipeRes.Msg);
        _ipcPipe = pipeRes.Value;

        var confRes = CreateWhisperConfigArg(_ipcPipe.GetPipeClientHandle());
        if (!confRes.IsOk) return ResC.Fail(confRes.Msg);

        var procPath = Path.Combine(PathUtils.PathExecutableFolder, "HoscyWhisperV2Process");
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo(procPath)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                ErrorDialog = false,
                Arguments = confRes.Value
            },
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += HandleConsoleOutput;
        process.ErrorDataReceived += HandleConsoleOutput;

        _startedSignalReceived = false;
        var procRes = ResC.TWrapR(process.Start, "Failed to start process", _logger);
        if (procRes.IsOk && procRes.Value)
        {
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            _whisperProcess = process;
        }

        if (_whisperProcess is null || OtherUtils.HasProcessExitedSafe(_whisperProcess))
        {
            var message = "Unable to start whisper process";
            _logger.Error(message);
            PerformCleanup();
            return ResC.Fail(procRes.IsOk ? ResMsg.Ftl(message) : procRes.Msg);
        }

        var started = OtherUtils.WaitWhile(() => { return !_startedSignalReceived; }, 5000, 10); 
        if (!started)
        {
            var message = "Did not receive startup signal from process";
            _logger.Error(message);
            PerformCleanup();
            return ResC.Fail(ResMsg.Err(message));
        }
        _whisperProcess.Exited += OnUnexpectedProcessExit;

        try
        {
            _ipcPipe.Start();
        } 
        catch (Exception ex)
        {
            _logger.Error(ex, "IPC pipe did not start correctly");
            PerformCleanup();
            throw;
        }

        _keepAlive = new(_logger, TimeSpan.FromSeconds(10));
        _keepAlive.OnKeepAliveFailed += OnKeepAliveFailed;
        _keepAlive.OnKeepAliveSend += SendKeepAlive;
        _keepAlive.Start();

        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override bool IsStarted()
        => _whisperProcess is not null || _ipcPipe is not null || _keepAlive is not null;
    protected override bool IsProcessing()
    {
        if (_ipcPipe is null || !_ipcPipe.IsPipeConnected || _keepAlive is null || _whisperProcess is null)
            return false;

        return !OtherUtils.HasProcessExitedSafe(_whisperProcess);
    }

    private Res<string> CreateWhisperConfigArg(string pipeHandleSend)
    {
        if (string.IsNullOrWhiteSpace(_config.Recognition_Whisper_SelectedModel) 
            || !_config.Recognition_Whisper_Models.TryGetValue(_config.Recognition_Whisper_SelectedModel, out var modelPath))
        {
            return ResC.TFailLog<string>($"Could not find whisper model in config with name \"{_config.Recognition_Whisper_SelectedModel}\"", _logger);
        }

        var args = new WhisperIpcConfig()
        {
            Input_GraceFramesForIrregularitiesBoundary = (uint)(_config.Recognition_Whisper_Cfg_DetectPauseDurationMs / WhisperIpcConfig.MS_IN_FRAME),
            Input_GraceFramesForIrregularitiesMiddle = (uint)(_config.Recognition_Whisper_Cfg_DetectOuterSilenceDurationMs / WhisperIpcConfig.MS_IN_FRAME),
            Input_MaxRecognitionFrames = (uint)(_config.Recognition_Whisper_Cfg_MaxSentenceDurationMs / WhisperIpcConfig.MS_IN_FRAME),
            Input_MinimumConsecutiveAudioFrames = (uint)(_config.Recognition_Whisper_Cfg_MinSentenceDurationMs / WhisperIpcConfig.MS_IN_FRAME),
            Input_RecognitionFrameInterval = (uint)(_config.Recognition_Whisper_Cfg_RecognitionUpdateIntervalMs / WhisperIpcConfig.MS_IN_FRAME),

            ParentProcessId = Process.GetCurrentProcess().Id,
            ParentSendingPipe = pipeHandleSend,

            CaptureDeviceName = _config.Audio_CurrentMicrophoneName,
            VadOperatingMode = _config.Recognition_Whisper_Cfg_VadOperatingMode,

            Whisper_DetectLanguage = _config.Recognition_Whisper_Cfg_DetectLanguage,
            Whisper_Language = _config.Recognition_Whisper_Cfg_Language,
            Whisper_ModelPath = modelPath,
            Whisper_SingleSegment = _config.Recognition_Whisper_Cfg_UseSingleSegmentMode,
            Whisper_TranslateToEnglish = _config.Recognition_Whisper_Cfg_TranslateToEnglish,
            Whisper_UseGpu = _config.Recognition_Whisper_Cfg_UseGpu,

            Whisper_BeamSize = _config.Recognition_Whisper_CfgAdv_BeamSize,
            Whisper_GpuId = _config.Recognition_Whisper_CfgAdv_GraphicsAdapterId,
            Whisper_GreedyBestOf = _config.Recognition_Whisper_CfgAdv_GreedyBestOf,
            Whisper_MaxInitialT = _config.Recognition_Whisper_CfgAdv_MaxInitialT,
            Whisper_MaxSegmentLength = _config.Recognition_Whisper_CfgAdv_MaxSegmentLength,
            Whisper_MaxTokensPerSegment = _config.Recognition_Whisper_CfgAdv_MaxTokensPerSegment, 
            Whisper_NoSpeechThreshold = _config.Recognition_Whisper_CfgAdv_NoSpeechThreshold,
            Whisper_Prompt = _config.Recognition_Whisper_CfgAdv_Prompt,
            Whisper_SetThreads = _config.Recognition_Whisper_CfgAdv_SetThreads,
            Whisper_Temperature = _config.Recognition_Whisper_CfgAdv_Temperature,
            Whisper_TemperatureInc = _config.Recognition_Whisper_CfgAdv_TemperatureInc,
            Whisper_ThreadCount = _config.Recognition_Whisper_CfgAdv_ThreadsUsed,
            Whisper_UseBeamSearchSampling = _config.Recognition_Whisper_CfgAdv_UseBeamSearchSampling,
            Whisper_UseGreedySampling = _config.Recognition_Whisper_CfgAdv_UseGreedySampling,

            Debug_LogVerboseExtra = _config.Debug_LogVerboseExtra
        };

        try
        {
            var argsJson = JsonConvert.SerializeObject(args, Formatting.None);
            var argBytes = Encoding.UTF8.GetBytes(argsJson);
            return ResC.TOk(Convert.ToBase64String(argBytes));
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<string>("Failed converting whisper config to string", _logger, ex);
        }
    }
    #endregion

    #region Stopping
    protected override Res StopForRecognitionModule()
    {
        if (_filteredActions.Count > 0)
        {
            LogFilteredActions();
        }
        PerformCleanup();
        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _keepAlive?.Dispose();
        _keepAlive = null;

        _ipcPipe?.Dispose();
        _ipcPipe = null;

        _whisperProcess?.Dispose();
        _whisperProcess = null;
    }

    private void OnKeepAliveFailed()
    {
        _logger.Warning("KeepAlive has failed, stopping");
        var res = Stop();
        if (!res.IsOk)
            _logger.Warning("KeepAlive stop has run into an error: {result}", res);
    }

    private void PerformCleanup()
    {
        _logger.Debug("Performing cleanup");

        if (_keepAlive is not null)
        {
            _keepAlive.Stop();
            _keepAlive.Dispose();
            _keepAlive = null;
        }

        var signalSent = SendWhisperProcessSignalIfNeeded();

        _ipcPipe?.Dispose();
        _ipcPipe = null;

        if (signalSent.HasValue)
        {
            WaitForWhisperProcessStop(signalSent.Value);
        } 
    }

    private void OnUnexpectedProcessExit(object? sender, EventArgs e)
    {
        _logger.Warning("Process stopped unexpectedly!");
        Stop().IfFail(x => _logger.Error("Stop failed after unexpected exit ({result})", x));
    }

    private bool? SendWhisperProcessSignalIfNeeded()
    {
        if (_whisperProcess is null) return null;

        if (OtherUtils.HasProcessExitedSafe(_whisperProcess))
        {
            _whisperProcess.Dispose();
            _whisperProcess = null;
            return null;
        }

        _whisperProcess.Exited -= OnUnexpectedProcessExit;
        var signalSent = _ipcPipe?.Enqueue(WhisperIpcStatus.IDENTIFIER, new WhisperIpcStatus(false)) ?? ResC.Fail("IPC pipe is null");
        if (!signalSent.IsOk)
        {
            _logger.Warning("Unable to queue stop signal to process ({info})",
                _ipcPipe is null ? "Pipe missing" : signalSent.ToString());
            return false;
        }
    
        var waitRes = OtherUtils.WaitWhile(() => _ipcPipe!.HasItemsQueued, 50, 5);
        if (!waitRes)
        {
            _logger.Warning("Unable to send stop signal to process in 50ms");
            return false;
        }

        return true;
    }

    private void WaitForWhisperProcessStop(bool signalSent)
    {
        try
        {
            WaitForProcessExit(signalSent);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Encountered an error stopping the whisper process");
        }

        _whisperProcess!.Dispose();
        _whisperProcess = null;
    }

    private void WaitForProcessExit(bool cancelSent)
    {
        if (cancelSent)
        {
            if (_whisperProcess!.WaitForExit(500))
            {
                _logger.Debug("Process has exited");
                return;
            }
        }

        _whisperProcess!.Kill();
        if (_whisperProcess.WaitForExit(500))
        {
            _notify.SendError("Error stopping Whisper Process", "The Whisper Process failed to properly close in 500ms, it might not exit");
            _logger.Error("Failed to exit whisper process within 500ms");
        }
    }
    #endregion

    #region IPC
    private void HandleConsoleOutput(object _, DataReceivedEventArgs args)
    {
        if (args.Data is null) return;
        
        var json = _ipcConverter.DeserializeBase64(args.Data);
        if (!json.IsOk || !_ipcConverter.IsValid(json.Value)) return;

        if (_config.Debug_LogVerboseExtra)
        {
            _logger.Verbose("Received \"{output}\" via IPC", json.Value);
        }
        var id = _ipcConverter.GetIdentifier(json.Value);
        switch (id)
        {
            case WhisperIpcLog.IDENTIFIER:
                var resLog = _ipcConverter.DeserializeJson<WhisperIpcLog>(json.Value);
                if (resLog.IsOk)
                {
                    var log = resLog.Value;
                    var text = log.Trace is null ? log.Message : $"{log.Message}\n{log.Trace}";
                    _logger.Write(log.LogLevel, $"Process: {text}");
                }
                return;

            case WhisperIpcRecognition.IDENTIFIER:
                var resRec = _ipcConverter.DeserializeJson<WhisperIpcRecognition>(json.Value);
                if (resRec.IsOk)
                {
                    ProcessReceivedRecognition(resRec.Value);
                }
                return;
                
            case WhisperIpcStatus.IDENTIFIER:
                var resSta = _ipcConverter.DeserializeJson<WhisperIpcStatus>(json.Value);
                if (resSta.IsOk && resSta.Value.State) 
                {
                    _logger.Debug("Received start signal from process");
                    _startedSignalReceived = true;
                }                
                return;

            case WhisperIpcKeepalive.IDENTIFIER:
                _keepAlive?.TriggerKeepAlive();
                return;

            case WhisperIpcMute.IDENTIFIER:
                var resMute = _ipcConverter.DeserializeJson<WhisperIpcMute>(json.Value);
                if (resMute.IsOk)
                {
                    _logger.Debug("Mute status received with value {value}", resMute.Value.State);
                    _muteSignalReceived = resMute.Value.State;
                }
                return;

            default: 
                _logger.Warning("Received unknown data with identifier {id}: \"{data}\"", id, json.Value);
                return;
        }
    }

    private void SendKeepAlive(uint index)
    {
        if (_ipcPipe is not null && _ipcPipe.CanEnqueue)
            _ipcPipe.Enqueue(WhisperIpcKeepalive.IDENTIFIER,new WhisperIpcKeepalive(index));
    }
    #endregion

    #region Listening
    private bool _listening = false;
    public override bool IsListening => _listening;

    private bool? _muteSignalReceived = null;
    protected override Res<bool> SetListeningForRecognitionModule(bool state)
    {
        var enqueueRes = _ipcPipe?.Enqueue(WhisperIpcMute.IDENTIFIER, new WhisperIpcMute(state)) 
            ?? ResC.Fail(ResMsg.Err("Pipe is missing"));
        if (!enqueueRes.IsOk)
        {
            _logger.Warning("Unable to send listening status, IPC failure (pipeExists={exists})", _ipcPipe is not null);
            return ResC.TFail<bool>(enqueueRes.Msg.WithContext("WhisperIPC"));
        }

        _muteSignalReceived = null;
        _logger.Debug("Sending mute signal and waiting for result");
        if (OtherUtils.WaitWhile(() => { return _muteSignalReceived is null; }, 50, 5))
        {
            _listening = _muteSignalReceived!.Value;
            return ResC.TOk(_listening);
        }
        else
        {
            return ResC.TFailLog<bool>("Failed to receive mute signal to process", _logger, lvl: ResMsgLvl.Warning);
        }
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion

    #region Output Processing
    private void ProcessReceivedRecognition(WhisperIpcRecognition recognition)
    {
        if (string.IsNullOrWhiteSpace(recognition.Text)) return;
        var text = recognition.Text.Trim();

        _logger.Verbose("Received raw recognition => Id={id}-{subId} Final={final} Text=\"{text}\"",
            recognition.Id, recognition.SubId, recognition.IsFinal, text);

        if (!ReplaceActions(ref text))
        {
            _logger.Verbose("Processed recognition actions => Id={id}-{subId} is empty",
                recognition.Id, recognition.SubId);
            return;
        }

        logger.Verbose("Processed recognition actions => Id={id}-{subId} Text=\"{text}\"",
                recognition.Id, recognition.SubId, text);

        if (!CleanText(ref text))
        {
            _logger.Verbose("Cleaned recognition => Id={id}-{subId} is empty",
                recognition.Id, recognition.SubId);
            return;
        }

        if (recognition.IsFinal)
        {
            logger.Debug("Cleaned recognition => Id={id}-{subId} Text=\"{text}\" => Is final, sending output and disabling activity",
                recognition.Id, recognition.SubId, text);
            
            InvokeSpeechActivity(false);
            InvokeSpeechRecognized(text);
        } 
        else
        {
            logger.Debug("Cleaned recognition => Id={id}-{subId} Text=\"{text}\" => Is not final, enabling activity only",
                recognition.Id, recognition.SubId, text);

            InvokeSpeechActivity(true);
        }
    }

    private static readonly Regex _actionDetector = new(@"( *)[\[\(\*] *([^\]\*\)]+) *[\*\)\]]");
    private readonly Dictionary<string, int> _filteredActions = [];

    /// <summary>
    /// Replaces all actions if valid
    /// </summary>
    /// <param name="text">Text to replace actions in</param>
    private bool ReplaceActions(ref string text)
    {
        var matches = _actionDetector.Matches(text);
        if ((matches?.Count ?? 0) == 0)
            return true;

        var sb = new StringBuilder(text);
        //Reversed so we can use sb.Remove()
        foreach (var match in matches!.Reverse())
        {
            var groupText = match.Groups[2].Value.ToLower();
            sb.Remove(match.Index, match.Length);

            var isValidAction = false;
            foreach (var filter in _config.Recognition_Whisper_Cfg_NoiseFilter.Values)
            {
                if (groupText.StartsWith(filter))
                {
                    isValidAction = true;
                    break;
                }
            }

            if (isValidAction)
            {
                if (_config.Recognition_Fixup_CapitalizeFirstLetter)
                    groupText = groupText.FirstCharToUpper();

                sb.Insert(match.Index, $"{match.Groups[1].Value}|{groupText}|");
            }
            else if (_config.Recognition_Whisper_Dbg_LogFilteredNoises && groupText != "BLANK_AUDIO")
            {
                //Adding it to the filtered list
                if (_filteredActions.TryGetValue(groupText, out var value))
                    _filteredActions[groupText] = value + 1;
                else
                    _filteredActions[groupText] = 1;
                _logger.Debug($"Noise \"{groupText}\" filtered out by whisper noise whitelist");
            }
        }

        text = sb.ToString();
        return !string.IsNullOrWhiteSpace(text);
    }

    private void LogFilteredActions()
    {
        var sortedActions = _filteredActions.Select(x => (x.Key, x.Value))
            .OrderByDescending(x => x.Value)
            .Select(x => $"\"{x.Key}\" ({x.Value}x)");
        _logger.Information("Filtered actions by Whisper: " + string.Join(", ", sortedActions));
    }

    /// <summary>
    /// Removes odd AI noise and replaces action indicator with an asterisk
    /// </summary>
    /// <param name="text">Text to clean</param>
    private bool CleanText(ref string text)
    {
        text = _config.Recognition_Whisper_Fix_RemoveRandomBrackets
            ? text.TrimStart(' ', '-', '(', '[', '*').TrimEnd()
            : text.TrimStart(' ', '-').TrimEnd();

        text.Replace('|', '*');
        return !string.IsNullOrWhiteSpace(text);
    }
    #endregion
}