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

namespace HoscyCore.Services.Recognition.Modules;

[PrototypeLoadIntoDiContainer(typeof(WhisperRecognitionModuleStartInfo), Lifetime.Singleton)]
public class WhisperRecognitionModuleStartInfo : IRecognitionModuleStartInfo //todo: Cleanup of process, output handling, config in CLI
{
    public string Name => "Whisper Recognizer";
    public string Description => "Local AI, quality / RAM, VRAM usage varies, startup may take a while";
    public Type ModuleType => typeof(WhisperRecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Microphone | RecognitionModuleConfigFlags.Whisper;
}

[PrototypeLoadIntoDiContainer(typeof(WhisperRecognitionModule), Lifetime.Transient)]
public class WhisperRecognitionModule(ILogger logger, ConfigModel config, IBackToFrontNotifyService notify)
    : RecognitionModuleBase(logger.ForContext<WhisperRecognitionModule>()) //todo: [REFACTOR++] Add disposable to classes like this and applu cleanup methods?
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
    protected override void StartForService()
    {
        _ipcPipe = new(_logger, _config.Debug_LogVerboseExtra);

        var procPath = Path.Combine(PathUtils.PathExecutableFolder, "HoscyWhisperV2Process"); //todo: [TEST] .exe needed on win?
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo(procPath)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                ErrorDialog = false,
                Arguments = CreateWhisperConfigArg(_ipcPipe.GetPipeClientHandle())
            },
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += HandleConsoleOutput;
        process.ErrorDataReceived += HandleConsoleOutput;

        _startedSignalReceived = false;
        if (process.Start())
        {
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            _whisperProcess = process;
        }
        if (_whisperProcess is null || _whisperProcess.HasExited)
        {
            _logger.Error("Unable to start whisper process");
            PerformCleanup();
            throw new StartStopServiceException($"Unable to start whisper process");
        }

        var started = OtherUtils.WaitWhile(() => { return !_startedSignalReceived; }, 5000, 10); 
        if (!started)
        {
            _logger.Error("Did not receive startup signal from process");
            PerformCleanup();
            throw new StartStopServiceException($"Did not receive startup signal from process");
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
        _keepAlive.OnKeepAliveFailed += Stop;
        _keepAlive.OnKeepAliveSend += SendKeepAlive;
        _keepAlive.Start();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override bool IsStarted()
        => _whisperProcess is not null || _ipcPipe is not null || _keepAlive is not null;
    protected override bool IsProcessing()
    {
        if (_ipcPipe is null || !_ipcPipe.IsPipeConnected || _keepAlive is null || _whisperProcess is null)
            return false;

        _whisperProcess.Refresh();
        return !_whisperProcess.HasExited;
    }

    private string CreateWhisperConfigArg(string pipeHandleSend)
    {
        if (string.IsNullOrWhiteSpace(_config.Recognition_Whisper_SelectedModel) 
            || !_config.Recognition_Whisper_Models.TryGetValue(_config.Recognition_Whisper_SelectedModel, out var modelPath))
        {
            _logger.Error("Could not find whisper model in config with name \"{modelName}\"", _config.Recognition_Whisper_SelectedModel);
            throw new StartStopServiceException($"Could not find whisper model in config with name \"{_config.Recognition_Whisper_SelectedModel}\"");
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

        var argsJson = JsonConvert.SerializeObject(args, Formatting.None);
        var argBytes = System.Text.Encoding.UTF8.GetBytes(argsJson);
        return Convert.ToBase64String(argBytes);
    }
    #endregion

    #region Stopping
    protected override void StopForRecognitionModule()
    {
        PerformCleanup();
        return;
    }

    private void PerformCleanup() //todo: [FIX] Cleanup is called twice because IPC sends stop signal
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
        Stop();
    }

    private bool? SendWhisperProcessSignalIfNeeded()
    {
        if (_whisperProcess is null) return null;

        _whisperProcess.Refresh();
        if (_whisperProcess.HasExited) //todo: [FIX] This can throw an exception????
        {
            _whisperProcess.Dispose();
            _whisperProcess = null;
            return null;
        }

        _whisperProcess.Exited -= OnUnexpectedProcessExit;
        var signalSent = _ipcPipe?.Enqueue(WhisperIpcStatus.IDENTIFIER, new WhisperIpcStatus(false)) ?? false;
        if (!signalSent)
        {
            _logger.Warning("Unable to queue stop signal to process (pipeExists={exists})", _ipcPipe is not null);
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
            _whisperProcess!.WaitForExit(500);
            _whisperProcess.Refresh();
            if (_whisperProcess.HasExited)
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
        if (args.Data is null || !_ipcConverter.IsValid(args.Data)) return;

        if (_config.Debug_LogVerboseExtra)
        {
            _logger.Verbose("Received \"{output}\" via IPC", args.Data);
        }
        var id = _ipcConverter.GetIdentifier(args.Data);
        switch (id)
        {
            case WhisperIpcLog.IDENTIFIER:
                if (_ipcConverter.TryDeserialize<WhisperIpcLog>(args.Data, out var resLog))
                {
                    var text = resLog.Trace is null ? resLog.Message : $"{resLog.Message}\n{resLog.Trace}";
                    _logger.Write(resLog.LogLevel, $"Process: {text}");
                }
                return;

            case WhisperIpcRecognition.IDENTIFIER:
                if (_ipcConverter.TryDeserialize<WhisperIpcRecognition>(args.Data, out var resRec))
                {
                    _logger.Debug($"{resRec.Id}-{resRec.SubId} {resRec.IsFinal}: {resRec.Text}");
                    //todo: handling and processing!
                }
                return;
                
            case WhisperIpcStatus.IDENTIFIER:
                if (_ipcConverter.TryDeserialize<WhisperIpcStatus>(args.Data, out var resSta)) 
                {
                    if (resSta.State)
                    {
                        _logger.Debug("Received start signal from process");
                        _startedSignalReceived = true;
                    }
                }                
                return;

            case WhisperIpcKeepalive.IDENTIFIER:
                _keepAlive?.TriggerKeepAlive();
                return;

            case WhisperIpcMute.IDENTIFIER:
                if (_ipcConverter.TryDeserialize<WhisperIpcMute>(args.Data, out var resMute))
                {
                    _logger.Debug("Mute status received with value {value}", resMute.State);
                    _muteSignalReceived = resMute.State;
                }
                return;

            default: 
                _logger.Warning("Received unknown data with identifier {id}: \"{data}\"", id, args.Data);
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
    protected override bool SetListeningForRecognitionModule(bool state)
    {
        if (_ipcPipe is null || !_ipcPipe.Enqueue(WhisperIpcMute.IDENTIFIER, new WhisperIpcMute(state)))
        {
            _logger.Warning("Unable to send listening status, IPC failure"); //todo: notify
            return IsListening;
        }

        _muteSignalReceived = null;
        _logger.Debug("Sending mute signal and waiting for result");
        if (OtherUtils.WaitWhile(() => { return _muteSignalReceived is null; }, 50, 5))
        {
            _listening = _muteSignalReceived!.Value;
        }
        else
        {
            _logger.Warning("Failed to receive mute signal"); //todo: notify
        }
        return IsListening;
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion
}