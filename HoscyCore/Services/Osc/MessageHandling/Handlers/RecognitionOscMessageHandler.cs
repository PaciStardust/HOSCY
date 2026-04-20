using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling.Handlers;

[PrototypeLoadIntoDiContainer(typeof(RecognitionOscMessageHandler), Lifetime.Transient)]
public class RecognitionOscMessageHandler(ILogger logger, IRecognitionManagerService recognition, ConfigModel config) : IOscMessageHandler
{
    private readonly ILogger _logger = logger.ForContext<RecognitionOscMessageHandler>();
    private readonly IRecognitionManagerService _recognition = recognition;
    private readonly ConfigModel _config = config;

    public bool HandleMessage(OscMessage message)
    {
        if (message.Address == _config.Osc_Address_Game_Mute)
        {
            if (!_config.Recognition_Mute_OnGameMute)
                return true;

            if (message.Arguments.Length == 0 || message.Arguments[0] is not bool state)
            {
                _logger.Warning("Received OSC message for game mute without state parameter");
                return true;
            }

            if (_recognition.GetCurrentModuleStatus() != Core.ServiceStatus.Stopped)
            {
                _recognition.SetListening(!state);
            }

            return true;
        }

        if (message.Address == _config.Osc_Address_Tool_ToggleMute)
        {
            _logger.Debug("Received OSC message for toggling recognition mute");
            _recognition.SetListening(!_recognition.IsListening);
            return true;
        }

        if (message.Address == _config.Osc_Address_Tool_ToggleRecognitionAutoMute)
        {
            _logger.Debug("Received OSC message for toggling automatic recognition mute");
            _config.Recognition_Mute_OnGameMute = !_config.Recognition_Mute_OnGameMute;
            return true;
        }

        return false;
    }
}