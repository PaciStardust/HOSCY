using System.Diagnostics;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Newtonsoft.Json;
using Serilog;

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
public class WhisperRecognitionModule(ILogger logger, ConfigModel config)
    : RecognitionModuleBase(logger.ForContext<WhisperRecognitionModule>()) //todo: [REFACTOR] Add disposable?
{
    private readonly ConfigModel _config = config;

    private Process? _whisperProcess = null;
    private bool _started = false; //todo: remove later

    protected override void StartForService()
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
            ParentReceivingPipe = string.Empty, //todo
            ParentSendingPipe = string.Empty, //todo

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
            Whisper_UseGreedySampling = _config.Recognition_Whisper_CfgAdv_UseGreedySampling
        };

        var argsJson = JsonConvert.SerializeObject(args, Formatting.None);
        var argBytes = System.Text.Encoding.UTF8.GetBytes(argsJson);
        var argText = Convert.ToBase64String(argBytes);

        var procPath = Path.Combine(PathUtils.PathExecutableFolder, "HoscyWhisperV2Process"); //todo: .exe needed on win?
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo(procPath)
            {
                CreateNoWindow = true,
                RedirectStandardError = true, //todo:
                RedirectStandardOutput = true, //todo:
                ErrorDialog = false,
                Arguments = argText
            },
            EnableRaisingEvents = true
        };

        if (process.Start())
        {
            _whisperProcess = process;
        }
        if (_whisperProcess is null || _whisperProcess.HasExited)
        {
            _logger.Error("Unable to start whisper process");
            throw new StartStopServiceException($"Unable to start whisper process");
        }

        /*
        TODO

        Step 1: Starting the process
        - keepalive timer + Mutex claim
        - On process do mutex checks
        - Proper shutdown again
        - (Listen to COUT anyways pre-ipc)

        Step 2: Init IPC

        Step 3: Actual IPC impl
        */



        // var ipcConfig = new WhisperIpcConfig() //todo: validate
        // {
        //     CaptureDeviceName = _config.Audio_CurrentMicrophoneName,
        //     Input_GraceFramesForIrregularitiesBoundary = ;
        // };

        // using var pipeReceive = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        // using var pipeSend = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);


        _started = true;
    }

    protected override void StopForRecognitionModule()
    {
        if (_whisperProcess is not null)
        {
            try //todo: notify?
            {
                if (!_whisperProcess.HasExited)
                {
                    _whisperProcess.Kill(); //todo: should be gently stopped before
                    if (_whisperProcess.WaitForExit(100)!)
                    {
                        _logger.Error("Failed to exit whisper process within 100ms"); 
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Encountered an error stopping the whisper process");
            }
            _whisperProcess.Dispose();
            _whisperProcess = null;
        }

        _started = false;
        return; //todo: impl
    }

    public override bool IsListening => true; //todo: impl

    protected override bool UseOnlySetListeningWhenStartedProtection => true; //todo: impl

    protected override bool UseAlreadyStartedProtection => true; //todo: impl

    protected override bool IsProcessing() //todo: impl
    {
        return _started;
    }

    protected override bool IsStarted() //todo: impl
    {
        return _started;
    }

    protected override bool SetListeningForRecognitionModule(bool state) //todo: impl
    {
        return true;
    }
}