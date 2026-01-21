using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Input;

[PrototypeLoadIntoDiContainer(typeof(IExternalInputService))]
public class ExternalInputService(ConfigModel config, IOutputManagerService output, ILogger logger) : IExternalInputService
{
    private readonly ConfigModel _config = config;
    private readonly IOutputManagerService _output = output;
    private readonly ILogger _logger = logger.ForContext<ExternalInputService>();

    public void SendMessage(string contents)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;
        var flags = GenerateFlags();
        _logger.Debug("Sending manual input message \"{message}\"", contents);
        _output.SendMessage(contents, flags);
        _logger.Debug("Sent manual input message \"{message}\"", contents);
    }

    public void SendNotification(string contents, OutputNotificationPriority prio = OutputNotificationPriority.Medium)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;
        var flags = GenerateFlags();
        _logger.Debug("Sending manual input notification \"{message}\"", contents);
        _output.SendNotification(contents, prio, flags);
        _logger.Debug("Sent manual input notification \"{message}\"", contents);
    }

    private OutputSettingsFlags GenerateFlags()
    {
        return (_config.ExternalInput_SendViaText ? OutputSettingsFlags.AllowTextOutput : OutputSettingsFlags.None)
            | (_config.ExternalInput_SendViaAudio ? OutputSettingsFlags.AllowAudioOutput : OutputSettingsFlags.None)
            | (_config.ExternalInput_SendViaOther ? OutputSettingsFlags.AllowOtherOutput : OutputSettingsFlags.None)
            | (_config.ExternalInput_CanBeTranslated ? OutputSettingsFlags.DoTranslate : OutputSettingsFlags.None)
            | (_config.ExternalInput_CanTriggerReplace ? OutputSettingsFlags.DoPreprocess : OutputSettingsFlags.None); //todo: [FEAT] Cut between Commands and Preprocess 
    }
}