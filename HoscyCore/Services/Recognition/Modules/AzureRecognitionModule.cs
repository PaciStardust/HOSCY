using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Utility;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Serilog;

namespace HoscyCore.Services.Recognition.Modules;

[PrototypeLoadIntoDiContainer(typeof(AzureRecognitionModuleStartInfo), Lifetime.Singleton)]
public class AzureRecognitionModuleStartInfo : IRecognitionModuleStartInfo //todo: [TEST] Test this once azure available
{
    public string Name => "Azure Recognizer";
    public string Description => "Remote recognition using Azure-API";
    public Type ModuleType => typeof(TestRecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Azure | RecognitionModuleConfigFlags.Microphone;
}

[PrototypeLoadIntoDiContainer(typeof(AzureRecognitionModule), Lifetime.Transient)]
public class AzureRecognitionModule(ILogger logger, ConfigModel config, IAudioService audio)
    : RecognitionModuleBase(logger.ForContext<AzureRecognitionModule>())
{
    #region Injects
    private readonly ConfigModel _config = config;
    private readonly IAudioService _audio = audio;
    #endregion

    #region Service Vars
    private SpeechRecognizer? _rec;
    private TaskCompletionSource<int>? _tcs;
    #endregion

    #region Start
    protected override Res StartForService()
    {
        var audioDeviceName = GetAudioDeviceName();
        if (!audioDeviceName.IsOk) return ResC.Fail(audioDeviceName.Msg);

        var audioConfigResult = ResC.TWrapR(() => AudioConfig.FromMicrophoneInput(audioDeviceName.Value),
            $"Failed to create audio config of microphone \"{audioDeviceName.Value}\"", _logger);

        var isMultilang = _config.Recognition_Azure_Languages.Count > 1;
        var speechConfigResult = CreateBaseSpeechConfig(isMultilang);
        if (!speechConfigResult.IsOk) return ResC.Fail(speechConfigResult.Msg);
        var speechConfig = speechConfigResult.Value;

        speechConfig.SetProfanity(_config.Recognition_Azure_CensorProfanity ? ProfanityOption.Masked : ProfanityOption.Raw);

        if (!string.IsNullOrWhiteSpace(_config.Recognition_Azure_CustomEndpoint))
            speechConfig.EndpointId = _config.Recognition_Azure_CustomEndpoint;

        Res<SpeechRecognizer> recResult;
        if (isMultilang) //this looks scuffed but is done as I think its quicker in api terms
        {
            var languages = ResC.TWrapR(() => AutoDetectSourceLanguageConfig.FromLanguages(_config.Recognition_Azure_Languages.ToArray()),
                "Failed to read languages", _logger);
            if (!languages.IsOk) return ResC.Fail(languages.Msg);

            speechConfig.SetProperty(PropertyId.SpeechServiceConnection_LanguageIdMode, "Continuous");
            recResult = ResC.TWrapR(() => new SpeechRecognizer(speechConfig, languages.Value, audioConfigResult.Value),
                "Failed to create multilang recognizer", _logger);
        }
        else
        {
            if (_config.Recognition_Azure_Languages.Count == 1)
                speechConfig.SpeechRecognitionLanguage = _config.Recognition_Azure_Languages.First();

            recResult = ResC.TWrapR(() => new SpeechRecognizer(speechConfig, audioConfigResult.Value),
                "Failed to create recognizer", _logger);
        }

        if (!recResult.IsOk) return ResC.Fail(recResult.Msg);
        _rec = recResult.Value;

        if (_config.Recognition_Azure_PresetPhrases.Count != 0)
        {
            var phraseListResult = ResC.TWrapR(() => PhraseListGrammar.FromRecognizer(_rec),
                "Failed to grab phrases", _logger);
            if (!phraseListResult.IsOk) return ResC.Fail(phraseListResult.Msg);

            foreach (var phrase in _config.Recognition_Azure_PresetPhrases)
                phraseListResult.Value.AddPhrase(phrase);
        }

        _rec.Recognized += OnRecognized;
        _rec.Canceled += OnCanceled;
        _rec.SessionStopped += OnStopped;
        _rec.SessionStarted += OnStarted;

        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    private Res<string> GetAudioDeviceName()
    {
        var deviceListResult = _audio.GetCaptureDevices();
        if (!deviceListResult.IsOk) return ResC.TFail<string>(deviceListResult.Msg);

        var devMatch = AudioUtils.FindDevice(deviceListResult.Value, _config.Audio_CurrentMicrophoneName, _logger);
        if (devMatch is null) return ResC.TFailLog<string>($"Microphone with name \"{_config.Audio_CurrentMicrophoneName}\" could not be found",
            _logger, lvl: ResMsgLvl.Warning);

        return ResC.TOk(devMatch.Value.Name);
    }

    private Res<SpeechConfig> CreateBaseSpeechConfig(bool isMultilang)
    {
        try
        {
            var conf = isMultilang
                ? SpeechConfig.FromEndpoint(new($"wss://{_config.AzureServices_Region}.stt.speech.microsoft.com/speech/universal/v2"),
                    _config.AzureServices_ApiKey)
                : SpeechConfig.FromSubscription(_config.AzureServices_ApiKey, _config.AzureServices_Region);
            return ResC.TOk(conf);
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<SpeechConfig>("Failed to create base speech config", _logger, ex);
        }
    }
    #endregion

    #region Stop / Info
    protected override bool IsStarted()
        => _rec is not null || _tcs is not null;

    protected override bool IsProcessing()
        => _rec is not null;

    protected override Res StopForRecognitionModule()
    {
        return StopRecognition();
    }
    protected override void DisposeCleanup()
    {
        _tcs = null;

        _rec?.Dispose();
        _rec = null;
    }
    #endregion

    #region Listening
    public override bool IsListening 
        => _tcs is not null;

    protected override Res<bool> SetListeningForRecognitionModule(bool state)
    {
        if (state)
        {
            if (_rec is null) 
                return ResC.TFail<bool>("Internal recognizer is not running");

            DoRecognizing().RunWithoutAwait();
        }
        else
        {
            var stop = StopRecognition();
            if (!stop.IsOk) return ResC.TFail<bool>(stop.Msg);
        }

        return ResC.TOk(IsListening);
    }
    private Res StopRecognition()
    {
        _logger.Debug("Stopping recognition...");

        _tcs?.TrySetResult(0);
        var waitRes = OtherUtils.WaitWhile(() => _tcs is not null, 500, 10);

        if (waitRes)
        {
            _logger.Debug("Stoped recognition");
            return ResC.Ok();
        }
        return ResC.FailLog("Failed to stop recognition", _logger);
    }

    private async Task DoRecognizing()
    {
        if (_rec is null || _tcs is not null)
            return;

        _logger.Debug("Starting recognition...");
        _tcs = new();

        try
        {
            await _rec.StartContinuousRecognitionAsync();
            _logger.Debug("Started recognition");
            await _tcs.Task.ConfigureAwait(false);
            await _rec.StopContinuousRecognitionAsync();
        }
        catch (Exception ex)
        {
            var res = ResC.FailLog("Failed to do recognition", _logger, ex);
            SetFault(res.Msg!);
            _tcs = null;
            InvokeInternalListeningStatusChange();
            return;
        }
        
        _tcs = null;
        _logger.Debug("Recognition finished");
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion

    #region Events
    private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
    {
        var result = e.Result.Text;
        if (string.IsNullOrWhiteSpace(result))
            return;

        _logger.Debug("Recognized message {message}", result);
        InvokeSpeechRecognized(result);
    }

    private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
    {
        if (e.ErrorCode != CancellationErrorCode.NoError)
        {
            var res = ResC.FailLog($"Recognition was cancelled (Reason: {CancellationReason.Error}, Code: {e.ErrorCode}, Details: {e.ErrorDetails})", _logger);
            SetFault(res.Msg!);
        }

        if (e.ErrorCode != CancellationErrorCode.ConnectionFailure)
        {
            return;
        }

        _logger.Warning("Stopping recognizer as connection has failed");
        Stop();
    }

    private void OnStopped(object? sender, SessionEventArgs e)
        => _logger.Debug("Recognition was stopped (Event)");
    private void OnStarted(object? sender, SessionEventArgs e)
        => _logger.Debug("Recognition was started (Event)");
    #endregion
}