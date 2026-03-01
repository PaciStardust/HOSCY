using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Network;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Components;
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
    private AudioCaptureDevice? _mic = null;
    private Recorder? _recorder = null;
    #endregion

    #region Start / Stop
    protected override bool IsStarted()
        => _stream is not null || _mic is not null || _recorder is not null;

    protected override bool IsProcessing()
        => _stream is not null && _mic is not null && _recorder is not null && _mic.IsRunning && _client.IsPresetLoaded();

    protected override void StartForService()
    {
        _logger.Debug("Initializing Api Client");

        var matchingModel = _config.Api_Presets
            .FirstOrDefault(x => x.Name == _config.Recognition_Api_Preset);
            
        if (matchingModel is null)
        {
            _logger.Error("Could not find preset \"{preset}\"", _config.Recognition_Api_Preset);
            throw new StartStopServiceException($"Could not find preset {_config.Recognition_Api_Preset}");
        }

        var loaded = _client.LoadPreset(matchingModel);
        if (!loaded)
        {
            _logger.Error("Could not find load \"{preset}\"", matchingModel.Name);
            throw new StartStopServiceException($"Could not load preset {matchingModel.Name}, check logs for more information");
        }

        _logger.Debug("Starting up audio");

        _stream = new();
        _mic = _audio.CreateCaptureDevice();
        _recorder = new Recorder(_mic, _stream)
        {
            ProcessCallback = OnRecorderProcessed
        };
        _mic.Start();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override void StopForRecognitionModule()
    {
        if (_recorder is not null)
        {
            if (_recorder.State != PlaybackState.Stopped)
                _recorder.StopRecording();
            _recorder.Dispose();
            _recorder = null;
        }

        if (_mic is not null)
        {
            _mic.Stop();
            _mic.Dispose();
            _mic = null;
        }

        _stream?.SetLength(0);
        _stream?.Close();
        _stream = null;

        if (_client.IsPresetLoaded())
        {
            _client.ClearPreset();
        }
    }
    #endregion

    #region Listening
    public override bool IsListening 
        =>  (_recorder?.State ?? PlaybackState.Stopped) != PlaybackState.Stopped;

    protected override bool SetListeningForRecognitionModule(bool state)
    {
        if (state)
            StartRecording();
        else
            StopRecording();

        return IsListening;
    }

    private DateTimeOffset _recordingStartedAt = DateTimeOffset.MaxValue;
    private void StartRecording()
    {
        if (_stream is null || _recorder is null || _mic is null)
        {
            var ex = new ArgumentException("Failed to start listening, some component is missing");
            _logger.Warning(ex, "Failed to start listening, some component is missing");
            SetFault(ex);
            return;
        }

        _logger.Debug("Starting listening and clearing stream");
        _stream.SetLength(0);

        try
        {
            _recordingStartedAt = DateTimeOffset.UtcNow;
            _recorder.StartRecording();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to start recording");
            SetFault(ex);
        }
    }

    private void StopRecording()
    {
        if (_stream is null || _recorder is null || _mic is null)
        {
            var ex = new ArgumentException("Some component is missing while stopping listening");
            _logger.Warning(ex, "Some component is missing while stopping listening");
            SetFault(ex);
        }

        try
        {
            _recorder?.StopRecording();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to start recording");
            SetFault(ex);
        }

        InvokeSpeechActivity(false);
        
        if (_stream is null || _stream.Length == 0)
        {
            _logger.Warning("No data available in stream");
            return;
        }

        try
        {
            _stream.Position = 0;
            RequestRecognition(_stream.GetBuffer()).RunWithoutAwait();
            _stream.SetLength(0);
        } 
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to start recording");
            SetFault(ex);
        }
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;

    private void OnRecorderProcessed(Span<float> _, Capability __)
    {
        if (_recordingStartedAt.AddSeconds(_config.Recognition_Api_MaxRecordingTime) > DateTimeOffset.Now)
        {
            _logger.Debug("Hit maximum recording time, cancelling");
            SetListening(false);
            return;
        }

        if (IsListening) InvokeSpeechActivity(true);
    }

    private async Task RequestRecognition(byte[] audioData)
    {
        var result = await _client.SendBytesAsync(audioData);
        InvokeSpeechRecognized(result);
    }
    #endregion
}