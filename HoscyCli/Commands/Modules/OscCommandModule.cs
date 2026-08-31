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

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["relay-ignore-if-handled"], nameof(ConfigModel.Osc_Relay_IgnoreIfHandled), ConfigModel.DESC_Osc_Relay_IgnoreIfHandled),
            new(["ip-out"], nameof(ConfigModel.Osc_Routing_TargetIp), ConfigModel.DESC_Osc_Routing_TargetIp),
            new(["port-out"], nameof(ConfigModel.Osc_Routing_TargetPort), ConfigModel.DESC_Osc_Routing_TargetPort)
        ];
    }

    [SubCommandModule(["status"], "Display overall OSC status")]
    public Res CmdDisplayStatus()
    {
        var relayError = _oscRelay.GetErrorMessageIfExists();

        var statusLines = new Dictionary<string, string>()
        {
            { "Relay Filters", relayError?.Message ?? "Working" }
        };

        var output = string.Join("\n", statusLines.Select(x => $"{x.Key,-16} | {x.Value}"));
        Console.WriteLine($"OSC Status:\n{output}");
        return ResC.Ok();
    }

    [SubCommandModule(["relay-filters"], ConfigModel.DESC_Osc_Relay_Filters)]
    public Res CmdEditRelayFilters()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Osc_Relay_Filters));
        if (!res.IsOk) return res;

        var resReload = _oscRelay.ReloadFilters();
        if (!resReload.IsOk) return ResC.Fail(resReload.Msg);

        var invalid = _oscRelay.GetInvalidFilterNames();
        if (invalid.Length > 0)
        {
            Console.WriteLine($"The following filters are invalid: {string.Join(", ", invalid)}");
        }
        
        return ResC.Ok();
    }

    [SubCommandModule(["port-in"], ConfigModel.DESC_Osc_Routing_ListenPort)]
    public Res CmdEditPortIn()
    {
        _reflectCm.SetProperty(nameof(ConfigModel.Osc_Routing_ListenPort));
        
        var res = _oscQuery.Stop();
        if (!res.IsOk) return res;

        res = _oscListen.Stop();
        if (!res.IsOk) return res;

        res = _oscListen.Start();
        if (!res.IsOk) return res;
        
        return _oscQuery.Start();
    }
}