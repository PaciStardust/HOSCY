using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Input;

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
        _logger.Debug("Forwarding manual input message \"{message}\"", contents);
        _output.SendMessage(contents, flags);
        _logger.Verbose("Forwarded manual input message \"{message}\"", contents);
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
    public void SendExternalTextMessage(string contents)
    {
        SendExternalMessage(contents, OutputSettingsFlags.AllowTextOutput, "text");
    }

    public void SendExternalAudioMessage(string contents)
    {
        SendExternalMessage(contents, OutputSettingsFlags.AllowAudioOutput, "audio");
    }

    public void SendExternalOtherMessage(string contents)
    {
        SendExternalMessage(contents, OutputSettingsFlags.AllowOtherOutput, "other");
    }

    private void SendExternalMessage(string contents, OutputSettingsFlags extraFlag, string logText)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;

        var flags = GenerateExternalProcessingFlags() | extraFlag;
        _logger.Debug("Forwarding external {logText} message \"{message}\"", logText, contents);
        _output.SendMessage(contents, flags);
        _logger.Verbose("Forwarded external {logText} message \"{message}\"", logText, contents);
    }

    public void SendExternalTextNotification(string contents, OutputNotificationPriority prio = OutputNotificationPriority.Medium)
    {
        if (string.IsNullOrWhiteSpace(contents)) return;

        var flags = GenerateExternalProcessingFlags() | OutputSettingsFlags.AllowTextOutput;
        _logger.Debug("Forwarding external text notification \"{message}\"", contents);
        _output.SendNotification(contents, prio, flags);
        _logger.Verbose("Forwarded external text notification \"{message}\"", contents);
    }

    private OutputSettingsFlags GenerateExternalProcessingFlags()
    {
        return (_config.ExternalInput_DoTranslate ? OutputSettingsFlags.DoTranslate : OutputSettingsFlags.None)
            | (_config.ExternalInput_DoPreprocessPartial ? OutputSettingsFlags.DoPreprocessPartial : OutputSettingsFlags.None)
            | (_config.ExternalInput_DoPreprocessFull ? OutputSettingsFlags.DoPreprocessFull : OutputSettingsFlags.None);
    }
    #endregion
}