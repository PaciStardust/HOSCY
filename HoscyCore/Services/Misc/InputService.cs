using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Misc;

[PrototypeLoadIntoDiContainer(typeof(IInputService))]
public class InputService(ConfigModel config, IOutputManagerService output, ILogger logger) : IInputService
{
    private readonly ConfigModel _config = config;
    private readonly IOutputManagerService _output = output;
    private readonly ILogger _logger = logger.ForContext<InputService>();

    #region Manual
    public void SendManualMessage(string contents)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;
        var flags = GenerateManualFlags();
        _logger.Debug("Sending manual input message \"{message}\"", contents);
        _output.SendMessage(contents, flags);
        _logger.Verbose("Sent manual input message \"{message}\"", contents);
    }

    private OutputSettingsFlags GenerateManualFlags()
    {
        return (_config.ManualInput_SendViaText ? OutputSettingsFlags.AllowTextOutput : OutputSettingsFlags.None)
            | (_config.ManualInput_SendViaAudio ? OutputSettingsFlags.AllowAudioOutput : OutputSettingsFlags.None)
            | (_config.ManualInput_SendViaOther ? OutputSettingsFlags.AllowOtherOutput : OutputSettingsFlags.None)
            | (_config.ManualInput_DoTranslate ? OutputSettingsFlags.DoTranslate : OutputSettingsFlags.None)
            | (_config.ManualInput_DoPreprocessPartial ? OutputSettingsFlags.DoPreprocessPartial : OutputSettingsFlags.None)
            | (_config.ManualInput_DoPreprocessFull ? OutputSettingsFlags.DoPreprocessFull : OutputSettingsFlags.None);
    }
    #endregion

    #region External
    public void SendExternalMessage(string contents)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;
        var flags = GenerateExternalFlags();
        _logger.Debug("Sending external input message \"{message}\"", contents);
        _output.SendMessage(contents, flags);
        _logger.Verbose("Sent external input message \"{message}\"", contents);
    }

    public void SendExternalNotification(string contents, OutputNotificationPriority prio = OutputNotificationPriority.Medium)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;
        var flags = GenerateExternalFlags();
        _logger.Debug("Sending external input notification \"{message}\"", contents);
        _output.SendNotification(contents, prio, flags);
        _logger.Verbose("Sent external input notification \"{message}\"", contents);
    }

    private OutputSettingsFlags GenerateExternalFlags()
    {
        return (_config.ExternalInput_SendViaText ? OutputSettingsFlags.AllowTextOutput : OutputSettingsFlags.None)
            | (_config.ExternalInput_SendViaAudio ? OutputSettingsFlags.AllowAudioOutput : OutputSettingsFlags.None)
            | (_config.ExternalInput_SendViaOther ? OutputSettingsFlags.AllowOtherOutput : OutputSettingsFlags.None)
            | (_config.ExternalInput_DoTranslate ? OutputSettingsFlags.DoTranslate : OutputSettingsFlags.None)
            | (_config.ExternalInput_DoPreprocessPartial ? OutputSettingsFlags.DoPreprocessPartial : OutputSettingsFlags.None)
            | (_config.ExternalInput_DoPreprocessFull ? OutputSettingsFlags.DoPreprocessFull : OutputSettingsFlags.None);
    }
    #endregion
}