using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;
using Microsoft.CognitiveServices.Speech;
using Serilog;

namespace HoscyCore.Services.Voice.Modules;

[LoadIntoDiContainer(typeof(AzureVoiceModuleStartInfo))]
public class AzureVoiceModuleStartInfo : IVoiceModuleStartInfo
{
    public VoiceModuleConfigFlags ConfigFlags => VoiceModuleConfigFlags.Azure;
    public string Name => "Azure Services";
    public string Description => "Voice synthesis using Azure";
    public Type ModuleType => typeof(AzureVoiceModule);
}

[PrototypeLoadIntoDiContainer(typeof(AzureVoiceModule), Lifetime.Transient)]
public class AzureVoiceModule //todo: [TEST] When azure keys available
(
    ILogger logger,
    ConfigModel config
) 
: VoiceModuleBase(logger.ForContext<AzureVoiceModule>())
{
    #region Injects
    private readonly ConfigModel _config = config;
    #endregion

    #region Vars
    private SpeechSynthesizer? _synth = null;
    #endregion

    #region Start / Stop
    protected override Res StartForService()
    {
        _logger.Debug("Connecting to Azure...");
        var configRes = ResC.TWrapR(() => SpeechConfig.FromSubscription(_config.AzureServices_ApiKey, _config.AzureServices_Region),
            "Failed to create SpeechConfig, were the correct credentials used?", _logger);
        if (!configRes.IsOk) return ResC.Fail(configRes.Msg);
        var speechCfg = configRes.Value;

        speechCfg.SetProfanity(_config.AzureServices_CensorProfanity ? ProfanityOption.Masked : ProfanityOption.Raw);
        
        if (!string.IsNullOrWhiteSpace(_config.Voice_Azure_CustomEndpoint))
            speechCfg.EndpointId = _config.Voice_Azure_CustomEndpoint;

        var currentVoice = _config.Voice_Azure_VoiceList.FirstOrDefault(x => x.Name == _config.Voice_Azure_CurrentVoice);
        if (currentVoice is not null)
        {
            if (!string.IsNullOrWhiteSpace(currentVoice.Voice))
                speechCfg.SpeechSynthesisVoiceName = currentVoice.Voice;
            if (!string.IsNullOrWhiteSpace(currentVoice.Language))
                speechCfg.SpeechSynthesisLanguage = currentVoice.Language;
        }
        else
        {
            _logger.Warning($"Unable to find azure voice with name \"{_config.Voice_Azure_CurrentVoice}\"");
        }

        speechCfg.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);

        var speechRes = ResC.TWrapR(() => new SpeechSynthesizer(speechCfg, null), 
            "Failed to create Azure synth, were the correct credentials used?", _logger);
        if (!speechRes.IsOk) return ResC.Fail(speechRes.Msg);
        _synth = speechRes.Value;

        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForModule() { return ResC.Ok(); }
    protected override void DisposeCleanup()
    {
        _synth?.Dispose();
        _synth = null;
    }

    protected override bool IsStarted() => _synth is not null;
    protected override bool IsProcessing() => IsStarted();
    #endregion

    #region Control
    public override async Task<Res> CreateAudio(string message, Stream stream, CancellationToken _) // Microslop does not offer any API to cancel
    {
        if (_synth == null) 
            return ResC.FailLog("Unable to create audio, synth is not set up", _logger, lvl: ResMsgLvl.Warning);

        var startTime = DateTime.Now;
        var resultRes = await ResC.TWrapRAsync(_synth.SpeakTextAsync(message), "Failed to create audio", _logger);
        if (!resultRes.IsOk) return ResC.Fail(resultRes.Msg);

        var result = resultRes.Value;
        switch (result.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                _logger.Debug($"Received TTS audio for \"{message}\" ({(DateTime.Now - startTime).TotalMilliseconds}ms) => {result.AudioDuration:mm\\:ss\\.fff}");
                stream.Write(result.AudioData);
                return ResC.Ok();

            case ResultReason.Canceled:
                var e = SpeechSynthesisCancellationDetails.FromResult(result);

                if (e.Reason == CancellationReason.Error)
                    return ResC.FailLog($"TTS \"{message}\" was cancelled (Reason: {CancellationReason.Error}, Code: {e.ErrorCode}, Details: {e.ErrorDetails})", _logger);
                else
                {
                    _logger.Debug($"TTS for \"{message}\" was cancelled (Reason: {CancellationReason.Error})");
                    return ResC.Ok();
                }
            default:
                return ResC.Ok();
        }
    }
    #endregion
}