using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Osc.Misc;
using HoscyCore.Services.Osc.Relay;
using HoscyCore.Services.Osc.SendReceive;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(OscCommandModule))]
public class OscCommandModule(IOscRelayService oscRelay, IOscListenService oscListen, IOscQueryService oscQuery, ReflectPropEditCommandModule reflectCm) : AttributeCommandModule
{
    private readonly IOscRelayService _oscRelay = oscRelay;
    private readonly IOscListenService _oscListen = oscListen;
    private readonly IOscQueryService _oscQuery = oscQuery;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    [SubCommandModule(["status"], "Display overall OSC status")]
    public CommandResult CmdDisplayStatus()
    {
        var relayError = _oscRelay.GetFaultIfExists();

        var statusLines = new Dictionary<string, string>()
        {
            { "Relay Filters", relayError?.Message ?? "Working" }
        };

        var output = string.Join("\n", statusLines.Select(x => $"{x.Key,-16} | {x.Value}"));
        Console.WriteLine($"OSC Status:\n{output}");
        return CommandResult.Success;
    }

    [SubCommandModule(["filters"], "Edit relay filters")]
    public CommandResult CmdEditRelayFilters()
    {
        _reflectCm.SetProperty(nameof(ConfigModel.Osc_Relay_Filters));
        _oscRelay.Restart();
        return CommandResult.Success;
    }

    [SubCommandModule(["ip-out"], "Edit the outbound ip")]
    public CommandResult CmdEditIpOut()
    {
        _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_TargetIp));
        return CommandResult.Success;
    }

    [SubCommandModule(["port-out"], "Edit the outbound port")]
    public CommandResult CmdEditPortOut()
    {
        _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_TargetPort));
        return CommandResult.Success;
    }

    [SubCommandModule(["port-in"], "Edit the inbound port")]
    public CommandResult CmdEditPortIn()
    {
        _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_ListenPort)); //todo: this is a bit scuffed
        _oscQuery.Stop();
        _oscListen.Restart(); 
        _oscQuery.Start();
        return CommandResult.Success;
    }
}