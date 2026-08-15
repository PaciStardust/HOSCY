using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Network;
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Voice.Modules;

[LoadIntoDiContainer(typeof(ApiVoiceModuleStartInfo))]
public class ApiVoiceModuleStartInfo : IVoiceModuleStartInfo
{
    public VoiceModuleConfigFlags ConfigFlags => VoiceModuleConfigFlags.Api;
    public string Name => "API";
    public string Description => "TTS using any API";
    public Type ModuleType => typeof(ApiVoiceModule);
    public ModulePriority Priority => ModulePriority.Low;
}

[PrototypeLoadIntoDiContainer(typeof(ApiVoiceModule), Lifetime.Transient)]
public class ApiVoiceModule(ILogger logger, ConfigModel config, IApiClient client) : VoiceModuleBase(logger.ForContext<ApiVoiceModule>())
{
    #region Inject
    private readonly ConfigModel _config = config;
    private readonly IApiClient _client = client;
    #endregion

    #region Start / Stop
    protected override Res StartForService() { return ResC.Ok(); }
    protected override bool UseAlreadyStartedProtection => false;
    protected override Res StopForModule() { return ResC.Ok(); }
    protected override void DisposeCleanup() { }
    protected override bool IsStarted() => true;
    protected override bool IsProcessing() => IsStarted();
    #endregion

    #region Sending
    public override async Task<Res> CreateAudio(string message, Stream stream, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_config.Voice_Api_Preset))
            return ResC.FailLog($"Not handling audio request for text \"{message}\", no preset set", _logger, lvl: ResMsgLvl.Warning);

        _logger.Debug("Sending audio request preset \"{preset}\" with contents \"{contents}\"",
            _config.Voice_Api_Preset, message);

        var idx = _config.Api_Presets_GetIndex(_config.Voice_Api_Preset);
        if (idx == -1)
        {
            return ResC.FailLog($"Failed to send audio request \"{message}\" via \"{_config.Voice_Api_Preset}\": Unable to locate preset",
                _logger, lvl: ResMsgLvl.Warning);
        }

        var preset = _client.LoadPreset(_config.Api_Presets[idx]);
        if (!preset.IsOk)
        {
            return ResC.FailLog($"Failed to send audio request \"{message}\" via \"{_config.Voice_Api_Preset}\": Preset is not valid",
                _logger, lvl: ResMsgLvl.Warning);
        }

        var audioRes = await _client.SendTextForBytesAsync(message);
        if (!audioRes.IsOk) return ResC.Fail(audioRes.Msg);

        stream.Write(audioRes.Value);
        return ResC.Ok();
    }
    #endregion
}