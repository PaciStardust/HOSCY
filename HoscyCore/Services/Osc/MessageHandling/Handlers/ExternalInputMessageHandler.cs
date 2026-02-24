using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Misc;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling.Handlers;

[PrototypeLoadIntoDiContainer(typeof(ExternalInputMessageHandler), Lifetime.Transient)]
public class ExternalInputMessageHandler(ConfigModel config, IInputService input, ILogger logger) : IOscMessageHandler
{
    private readonly ConfigModel _config = config;
    private readonly IInputService _input = input;
    private readonly ILogger _logger = logger;

    public bool HandleMessage(OscMessage message)
    {
        if(message.Address.Equals(_config.Osc_Address_Input_TextMessage, StringComparison.OrdinalIgnoreCase))
        {
            SendMessage(message, SendMode.TextMessage);
            return true;
        }

        if (message.Address.Equals(_config.Osc_Address_Input_TextNotification, StringComparison.OrdinalIgnoreCase))
        {
            SendMessage(message, SendMode.TextNotification);
            return true;
        }

        if (message.Address.Equals(_config.Osc_Address_Input_AudioMessage, StringComparison.OrdinalIgnoreCase))
        {
            SendMessage(message, SendMode.AudioMessage);
            return true;
        }

        if (message.Address.Equals(_config.Osc_Address_Input_OtherMessage, StringComparison.OrdinalIgnoreCase))
        {
            SendMessage(message, SendMode.OtherMessage);
            return true;
        }
        
        return false;
    }

    private void SendMessage(OscMessage message, SendMode mode)
    {
        _logger.Debug("Received external input packet (\"{address}\")", message.Address);

        if (message.Arguments.Length == 0 || message.Arguments[0] is not string contents) {
            _logger.Warning("External input packet did not have string as first arg");
            return;
        }

        if (string.IsNullOrWhiteSpace(contents)) return;

        _logger.Verbose("External input packet \"{address}\" is set to \"{contents}\"", message.Address, contents);
        switch (mode)
        {
            case SendMode.TextMessage:
                _input.SendExternalTextMessage(contents);
                break;
            case SendMode.TextNotification:
                _input.SendExternalTextNotification(contents);
                break;
            case SendMode.AudioMessage:
                _input.SendExternalAudioMessage(contents);
                break;
            case SendMode.OtherMessage:
                _input.SendExternalOtherMessage(contents);
                break;
        }
        _logger.Verbose("Handled external input packet \"{address}\" set to \"{contents}\"", message.Address, contents);
    }

    private enum SendMode
    {
        TextMessage,
        TextNotification,
        AudioMessage,
        OtherMessage
    }
}