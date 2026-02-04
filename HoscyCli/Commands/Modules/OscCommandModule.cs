using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Osc.Query;
using HoscyCore.Services.Osc.Relay;
using HoscyCore.Services.Osc.SendReceive;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(OscCommandModule))]
public class OscCommandModule(IOscRelayService oscRelay, IOscListenService oscListen, IOscQueryService oscQuery, ReflectPropEditCommandModule reflectCm)
    : AttributeCommandModule, ICoreCommandModule
{
    private readonly IOscRelayService _oscRelay = oscRelay;
    private readonly IOscListenService _oscListen = oscListen;
    private readonly IOscQueryService _oscQuery = oscQuery;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "OSC";
    public string ModuleDescription => "Configure OSC";
    public string[] ModuleCommands => ["osc"];

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

    [SubCommandModule(["relay-filters"], "Edit relay filters")]
    public CommandResult CmdEditRelayFilters()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Osc_Relay_Filters));
        _oscRelay.Restart();
        return res;
    }

    [SubCommandModule(["relay-ignore-if-handled"], "Edit relay ignore if handled")]
    public CommandResult CmdEditRelayIgnoreIfHandled()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Relay_IgnoreIfHandled));
    }

    [SubCommandModule(["ip-out"], "Edit the outbound ip")]
    public CommandResult CmdEditIpOut()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_TargetIp));
    }

    [SubCommandModule(["port-out"], "Edit the outbound port")]
    public CommandResult CmdEditPortOut()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_TargetPort));
    }

    [SubCommandModule(["port-in"], "Edit the inbound port")]
    public CommandResult CmdEditPortIn()
    {
        _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_ListenPort));
        _oscQuery.Stop();
        _oscListen.Restart(); 
        _oscQuery.Start();
        return CommandResult.Success;
    }
}