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

    public override Task HandleNotification(string contents, OutputNotificationPriority priority)
    {
        SendInternal(_config.ApiOut_Preset_Notification, "Notification",
            _config.ApiOut_PrependNotificationPriority ? $"{priority} > {contents}" : contents);
        return Task.CompletedTask;
    }

    public override void SetProcessingIndicator(bool isProcessing)
    {
        SendInternal(_config.ApiOut_Preset_Processing, "Notification",
            isProcessing ? _config.ApiOut_Value_True : _config.ApiOut_Value_False);
    }

    private void SendInternal(string presetName, string actionForLog, string contents)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            _logger.Debug("Not handling {cmd} command, no preset set", actionForLog);
            return;
        }

        _logger.Debug("Sending {action} for preset \"{preset}\" with contents \"{contents}\"",
            actionForLog, presetName, contents);

        var idx = _config.Api_Presets_GetIndex(presetName);
        if (idx == -1)
        {
            var res = ResC.FailLog($"Failed to send {actionForLog} \"{contents}\" via \"{presetName}\": Unable to locate preset",
                _logger, lvl: ResMsgLvl.Warning);
            SetFault(res.Msg!);
            return;
        }

        var preset = _client.LoadPreset(_config.Api_Presets[idx]);
        if (!preset.IsOk)
        {
            var res = ResC.FailLog($"Failed to send {actionForLog} \"{contents}\" via \"{presetName}\": Preset is not valid",
                _logger, lvl: ResMsgLvl.Warning);
            SetFault(res.Msg!);
            return;
        }

        _client.SendTextAsync(contents)
            .ContinueWith((x, _) => OnSendTaskComplete(x, actionForLog, presetName, contents), TaskContinuationOptions.None)
            .RunWithoutAwait();

        _logger.Debug("Sent {action} for preset \"{preset}\" with contents \"{contents}\"",
            actionForLog, presetName, contents);
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
    #endregion
}