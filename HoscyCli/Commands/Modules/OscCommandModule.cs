using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Osc.Query;
using HoscyCore.Services.Osc.Relay;
using HoscyCore.Services.Osc.SendReceive;
using HoscyCore.Utility;

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
    public Res CmdDisplayStatus()
    {
        var relayError = _oscRelay.GetFaultIfExists();

        var statusLines = new Dictionary<string, string>()
        {
            { "Relay Filters", relayError?.Message ?? "Working" }
        };

        var output = string.Join("\n", statusLines.Select(x => $"{x.Key,-16} | {x.Value}"));
        Console.WriteLine($"OSC Status:\n{output}");
        return ResC.Ok();
    }

    [SubCommandModule(["relay-filters"], "Edit relay filters")]
    public Res CmdEditRelayFilters()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Osc_Relay_Filters));
        if (!res.IsOk) return res;
        return _oscRelay.Restart();
    }

    [SubCommandModule(["relay-ignore-if-handled"], "Edit relay ignore if handled")]
    public Res CmdEditRelayIgnoreIfHandled()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Relay_IgnoreIfHandled));
    }

    [SubCommandModule(["ip-out"], "Edit the outbound ip")]
    public Res CmdEditIpOut()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_TargetIp));
    }

    [SubCommandModule(["port-out"], "Edit the outbound port")]
    public Res CmdEditPortOut()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_TargetPort));
    }

    [SubCommandModule(["port-in"], "Edit the inbound port")]
    public Res CmdEditPortIn()
    {
        _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_ListenPort));
        
        var res = _oscQuery.Stop();
        if (!res.IsOk) return res;

        res = _oscListen.Restart();
        if (!res.IsOk) return res;
        
        return _oscQuery.Start();
    }
}