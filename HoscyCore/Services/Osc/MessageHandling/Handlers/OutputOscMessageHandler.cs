using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Osc.MessageHandling.Core;
using HoscyCore.Services.Output.Core;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling.Handlers;

[PrototypeLoadIntoDiContainer(typeof(OutputOscMessageHandler))]
public class OutputOscMessageHandler(ConfigModel config, ILogger logger, IOutputManagerService output) : IOscMessageHandler
{
    private readonly ConfigModel _config = config;
    private readonly ILogger _logger = logger.ForContext<OutputOscMessageHandler>();
    private readonly IOutputManagerService _output = output;

    public bool HandleMessage(OscMessage message)
    {
        if (message.Address.Equals(_config.Osc_Address_Tool_ToggleReplacementsFull, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Debug("OSC set full replacements to {state}", !_config.Preprocessing_DoReplacementsFull);
            _config.Preprocessing_DoReplacementsFull = !_config.Preprocessing_DoReplacementsFull;
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Tool_ToggleReplacementsPartial, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Debug("OSC set partial replacements to {state}", !_config.Preprocessing_DoReplacementsPartial);
            _config.Preprocessing_DoReplacementsPartial = !_config.Preprocessing_DoReplacementsPartial;
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Tool_Clear, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Debug("Received OSC address to invoke clear");
            _output.Clear();
        }
        return false;
    }
}