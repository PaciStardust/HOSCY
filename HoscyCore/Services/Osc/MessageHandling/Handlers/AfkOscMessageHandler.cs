using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Misc;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling.Handlers;

[PrototypeLoadIntoDiContainer(typeof(AfkOscMessageHandler))]
public class AfkOscMessageHandler(ILogger logger, IAfkService afkService, ConfigModel config) : IOscMessageHandler //todo: [TEST] Does this trigger?
{
    private readonly IAfkService _afkService = afkService;
    private readonly ILogger _logger = logger.ForContext<AfkOscMessageHandler>();
    private readonly ConfigModel _config = config;

    public bool HandleMessage(OscMessage message)
    {
        if(!message.Address.Equals(_config.Osc_Address_Game_Afk, StringComparison.OrdinalIgnoreCase))
            return false;

        _logger.Information("Received OSC AFK packet");
        if (message.Arguments.Length == 0 || message.Arguments[0] is not bool afkState) {
            _logger.Warning("OSC AFK packet did not have bool as first arg");
            return true;
        }
        _logger.Debug("OSC AFK packet is set to {afkState}", afkState);
        if (afkState)
        {
            _afkService.StartAfk();
        } else
        {
            _afkService.StopAfk();
        }
        _logger.Information("Handled OSC AFK packet");
        return true;
    }
}