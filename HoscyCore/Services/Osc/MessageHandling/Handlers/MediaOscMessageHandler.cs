using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Media.Core;
using HoscyCore.Services.Osc.MessageHandling.Core;
using HoscyCore.Utility;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling.Handlers;

[LoadIntoDiContainer(typeof(MediaOscMessageHandler))] //todo: [TEST]
public class MediaOscMessageHandler
(
    ConfigModel config,
    IMediaControlService media,
    ILogger logger
) 
    : IOscMessageHandler
{
    private readonly ConfigModel _config = config;
    private readonly IMediaControlService _media = media;
    private readonly ILogger _logger = logger.ForContext<MediaOscMessageHandler>();

    public bool HandleMessage(OscMessage message)
    {
        if (message.Address.Equals(_config.Osc_Address_Media_Pause, StringComparison.OrdinalIgnoreCase))
        {
            LogCommand("Pause");
            _media.PauseAsync().RunWithoutAwait();
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Media_Rewind, StringComparison.OrdinalIgnoreCase))
        {
            LogCommand("Previous");
            _media.PreviousAsync().RunWithoutAwait();
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Media_Skip, StringComparison.OrdinalIgnoreCase))
        {
            LogCommand("Next");
            _media.NextAsync().RunWithoutAwait();
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Media_Toggle, StringComparison.OrdinalIgnoreCase))
        {
            LogCommand("PlayPause");
            _media.PlayPauseAsync().RunWithoutAwait();
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Media_Unpause, StringComparison.OrdinalIgnoreCase))
        {
            LogCommand("Play");
            _media.PlayAsync().RunWithoutAwait();
            return true;
        }
        return false;
    }

    private void LogCommand(string action)
    {
            _logger.Debug("Received media command {command}", action);
    }
}