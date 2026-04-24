using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Network;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Output.Handlers;

[LoadIntoDiContainer(typeof(ApiOutputHandlerStartInfo))]
public class ApiOutputHandlerStartInfo(ConfigModel config) : IOutputHandlerStartInfo
{
    private readonly ConfigModel _config = config;

    public Type ModuleType 
        => typeof(ApiOutputHandler);

    public bool ShouldBeEnabled()
        => _config.ApiOut_Enabled;
}

[PrototypeLoadIntoDiContainer(typeof(ApiOutputHandler), Lifetime.Transient)] //todo: [TEST] Write tests for this
public class ApiOutputHandler(ILogger logger, IApiClient client, ConfigModel config) : OutputHandlerBase(logger.ForContext<ApiOutputHandler>())
{
    private readonly IApiClient _client = client;
    private readonly ConfigModel _config = config;

    public override string Name => "API Output Module";
    public override OutputsAsMediaFlags OutputTypeFlags => OutputsAsMediaFlags.OutputsAsOther;

    #region Start / Stop
    protected override Res StartForService() 
    { 
        _client.ClearPreset();
        return Res.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForModule()
    {
        _client.ClearPreset();
        return Res.Ok();
    }
    protected override void DisposeCleanup() { }

    protected override bool IsStarted() => true;
    protected override bool IsProcessing() => true;
    #endregion

    #region Sending
    public override OutputTranslationFormat GetTranslationOutputMode()
        => _config.ApiOut_TranslationFormat;

    public override void Clear()
    {
        SendInternal(_config.ApiOut_Preset_Clear, "Clear", string.Empty);
    }

    public override Task HandleMessage(string contents)
    {
        SendInternal(_config.ApiOut_Preset_Message, "Message", contents);
        return Task.CompletedTask;
    }

    public override Task HandleNotification(string contents, OutputNotificationPriority priority) //todo: [FEAT] Implement priority?
    {
        SendInternal(_config.ApiOut_Preset_Notification, "Notification", contents);
        return Task.CompletedTask;
    }

    public override void SetProcessingIndicator(bool isProcessing)
    {
        SendInternal(_config.ApiOut_Preset_Processing, "Notification",
            isProcessing ? _config.ApiOut_Value_True : _config.ApiOut_Value_False);
    }

    private void SendInternal(string presetName, string actionForLog, string contents) //todo: [FEAT] Logging
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            _logger.Debug("Not handling {cmd} command, no preset set", actionForLog);
            return;
        }

        var idx = _config.Api_Presets_GetIndex(presetName); //todo: [REFACTOR] Replace with result and actually make use of it
        if (idx == -1) return; //todo: [FIX] make error

        var preset = _client.LoadPreset(_config.Api_Presets[idx]);
        if (!preset.IsOk) return; //todo: [FIX] make error

        _client.SendTextAsync(contents).RunWithoutAwait(); //todo: [FIX] make error?
    }
    #endregion
}