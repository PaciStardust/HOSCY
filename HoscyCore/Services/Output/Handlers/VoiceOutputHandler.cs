using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Output.Handlers;

[LoadIntoDiContainer(typeof(VoiceOutputHandlerStartInfo))]
public class VoiceOutputHandlerStartInfo(ConfigModel config) : IOutputHandlerStartInfo
{
    private readonly ConfigModel _config = config;

    public Type ModuleType => typeof(VoiceOutputHandler);
    public bool ShouldBeEnabled()
        => _config.Output_Voice_Enabled;
}

[PrototypeLoadIntoDiContainer(typeof(VoiceOutputHandler), Lifetime.Transient)]
public class VoiceOutputHandler(ILogger logger, IVoiceManagerService manager, ConfigModel config)
    : OutputHandlerBase(logger.ForContext<VoiceOutputHandler>())
{
    #region Vars
    private readonly IVoiceManagerService _manager = manager;
    private readonly ConfigModel _config = config;
    #endregion

    #region Info
    public override string Name => "Voice";
    public override OutputsAsMediaFlags OutputTypeFlags => OutputsAsMediaFlags.OutputsAsAudio;
    public override OutputTranslationFormat GetTranslationOutputMode() 
        => _config.Output_Voice_SendTranslated ? OutputTranslationFormat.Translation : OutputTranslationFormat.Untranslated;
    #endregion

    #region Start / Stop
    protected override Res StartForService() { return ResC.Ok(); }
    protected override bool UseAlreadyStartedProtection => false;
    protected override Res StopForModule() { return ResC.Ok(); }
    protected override void DisposeCleanup() { }

    protected override bool IsStarted() => true;
    protected override bool IsProcessing() 
        => IsStarted() && _manager.GetCurrentModuleStatus() == Services.Core.ServiceStatus.Processing;
    #endregion

    #region Functionality
    public override void Clear()
    {
        _logger.Debug("Forwarding clear command to manager");
        _manager.Clear();
    }

    public override Task HandleMessage(string contents)
    {
        _logger.Debug("Forwarding message \"{message}\" to manager", contents);
        var res = _manager.Enqueue(contents);
        SetFault(res.Msg);
        return Task.CompletedTask;
    }

    public override void SetProcessingIndicator(bool isProcessing) { return; }
    public override Task HandleNotification(string contents, OutputNotificationPriority priority)
        => Task.CompletedTask;
    #endregion
}