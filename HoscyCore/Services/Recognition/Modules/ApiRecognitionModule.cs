using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Network;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Enums;

namespace HoscyCore.Services.Recognition.Modules;

[PrototypeLoadIntoDiContainer(typeof(ApiRecognitionModuleStartInfo), Lifetime.Singleton)]
public class ApiRecognitionModuleStartInfo : IRecognitionModuleStartInfo
{
    public string Name => "Any-Api Recognizer";
    public string Description => "Remote recognition using Any-API, not continuous";
    public Type ModuleType => typeof(ApiRecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Microphone | RecognitionModuleConfigFlags.AnyApi;
}

[PrototypeLoadIntoDiContainer(typeof(ApiRecognitionModule), Lifetime.Transient)]
public class ApiRecognitionModule //todo: [TEST] does this work?
(
    ILogger logger,
    IApiClient apiClient,
    IAudioService audio,
    ConfigModel config
)
    : RecognitionModuleBase(logger.ForContext<ApiRecognitionModule>())
{
    #region Vars
    private readonly IApiClient _client = apiClient;
    private readonly IAudioService _audio = audio;
    private readonly ConfigModel _config = config;

    private MemoryStream? _stream = null;
    private AudioCaptureDeviceProxy? _mic = null;
    #endregion

    #region Start / Stop
    protected override bool IsStarted()
        => _stream is not null || _mic is not null ;

    protected override bool IsProcessing()
        => _stream is not null && _mic is not null && _mic.IsStarted && _client.IsPresetLoaded();

    protected override Res StartForService()
    {
        _logger.Debug("Initializing Api Client");

        var matchingModel = _config.Api_Presets
            .FirstOrDefault(x => x.Name == _config.Recognition_Api_Preset);
        if (matchingModel is null)
        {
            return ResC.FailLog($"Could not find preset \"{_config.Recognition_Api_Preset}\"", _logger);
        }

        var loaded = _client.LoadPreset(matchingModel);
        if (!loaded.IsOk)
        {
            _logger.Error("Could not find load \"{preset}\"", matchingModel.Name);
            return loaded;
        }

        _logger.Debug("Starting up audio");

        _stream = new();

        var micResult = _audio.CreateCaptureDeviceProxy();
        if (!micResult.IsOk) return ResC.Fail(micResult.Msg);

        _mic = micResult.Value;
        _mic.OnAudioProcessed += OnAudioProcessed;

        return _mic.Start();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForRecognitionModule()
    {
        List<ResMsg> fails = []; 

        if (_mic is not null)
        {
            _mic.SetListening(false);
            _mic.Stop().IfFail((x) => fails.Add(x.WithContext("Mic Stop")));
            
        }

        if (_client.IsPresetLoaded())
        {
            _client.ClearPreset();
        }

        return fails.Count == 0 ? ResC.Ok() : ResC.FailM(fails);
    }
    protected override void DisposeCleanup()
    {
        _mic?.Dispose();
        _mic = null;

        _stream?.SetLength(0);
        _stream?.Close();
        _stream = null;
    }
    #endregion

    #region Listening
    public override bool IsListening 
        =>  _mic?.IsListening ?? false;

    protected override Res<bool> SetListeningForRecognitionModule(bool state)
    {
        var res = state ? StartRecording() : StopRecording();
        return res.IsOk ? ResC.TOk(IsListening) : ResC.TFail<bool>(res.Msg);
    }

    private DateTimeOffset _recordingStartedAt = DateTimeOffset.MaxValue;
    private Res StartRecording()
    {
        if (_stream is null || _mic is null)
        {
            var res = ResMsg.Wrn("Failed to start listening, some component is missing");
            SetFaultLogNotify(res, "Recording start failed", null, _logger);
            return ResC.Fail(res);
        }

        _logger.Debug("Starting listening and clearing stream");
        InitStream(_stream);

        _recordingStartedAt = DateTimeOffset.UtcNow;
        _mic.SetListening(true);
        return ResC.Ok();
    }

    private Res StopRecording()
    {
        _stream?.Position = 0;
        var streamContents = _stream?.GetBuffer().ToArray();
        _mic?.SetListening(false);
        InitStream(_stream);

        InvokeSpeechActivity(false);
        
        if (streamContents is null || streamContents.Length == 0)
        {
            _logger.Warning("No data available in stream");
            return ResC.Ok();
        }

        try
        {
            AudioUtils.WriteRestOfWavHeader(streamContents);
            RequestRecognition(streamContents).RunWithoutAwait();
            InitStream(_stream);
            return ResC.Ok();
        } 
        catch (Exception ex)
        {
            var res = ResC.FailLog("Failed to stop listening", _logger, ex, ResMsgLvl.Warning);
            SetFault(res.Msg);
            return res;
        }
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;

    private void OnAudioProcessed(Span<byte> span, Capability capability)
    {
        if (!IsListening) return;

        if (_recordingStartedAt.AddSeconds(_config.Recognition_Api_MaxRecordingTime) < DateTimeOffset.UtcNow)
        {
            _logger.Debug("Hit maximum recording time, cancelling");
            SetListening(false);
            InvokeInternalListeningStatusChange();
            return;
        }

        InvokeSpeechActivity(true); //todo: [FIX] This needs a cooldown
        try
        {
            _stream?.Write(span);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to write to audio stream");
        }
    }

    private int _recId = 0;
    private async Task RequestRecognition(byte[] audioData)
    {
        var recId = ++_recId;
        _logger.Debug("Requesting recognition result for ID {recId}", recId);
        var result = await _client.SendBytesAsync(audioData);

        if (result.IsOk)
        {
            _logger.Debug("Received text \"{text}\" for ID {recId}", result.Value, recId);
            InvokeSpeechRecognized(result.Value);
        }
        else
        {
            _logger.Debug("Failed to receive for ID {recId} ({result})", recId, result);
            SetFault(result.Msg);
        }
    }

    private Task OnSendTaskComplete(Task<Res<string>> task, string actionForLog, string presetName, string contents)
    {
        if (task.IsFaulted)
        {
            var msg = ResC.FailLog($"Failed to send {actionForLog} \"{contents}\" via \"{presetName}\" for unknown reason", _logger, task.Exception);
            SetFault(msg.Msg!);
        }
        else if (task.IsCompletedSuccessfully && task.Result is not null && !task.Result.IsOk)
        {
            _logger.Warning($"Failed to send {actionForLog} \"{contents}\" via \"{presetName}\" with result: {task.Result.Msg}");
            SetFault(task.Result.Msg);
        } 
        return Task.CompletedTask;
    }

    private void InitStream(MemoryStream? stream)
    {
        stream?.SetLength(0);
        stream?.Write(AudioUtils.BaseWavHeader);
    }
    #endregion
}