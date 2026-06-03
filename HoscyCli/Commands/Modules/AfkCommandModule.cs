using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Afk;
using Serilog;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(AfkCommandModule))]
public class AfkCommandModule(IAfkService afkService, ILogger logger, ReflectPropEditCommandModule reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly IAfkService _afkService = afkService;
    private readonly ILogger _logger = logger.ForContext<AfkCommandModule>();
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "Afk";
    public string ModuleDescription => "Configure AFK detection and status";
    public string[] ModuleCommands => ["afk"];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["enabled"], nameof(ConfigModel.Afk_ShowDuration), ConfigModel.DESC_Afk_ShowDuration),
            new(["interval"], nameof(ConfigModel.Afk_BaseDurationDisplayIntervalSeconds), ConfigModel.DESC_Afk_BaseDurationDisplayIntervalSeconds),
            new(["double-time"], nameof(ConfigModel.Afk_TimesDisplayedBeforeDoublingInterval), ConfigModel.DESC_Afk_TimesDisplayedBeforeDoublingInterval),
            new(["txt-start"], nameof(ConfigModel.Afk_StartText), ConfigModel.DESC_Afk_StartText),
            new(["txt-status"], nameof(ConfigModel.Afk_StatusText), ConfigModel.DESC_Afk_StatusText),
            new(["txt-stop"], nameof(ConfigModel.Afk_StopText), ConfigModel.DESC_Afk_StopText)
        ];
    }

    [SubCommandModule(["status"], "Get service status")]
    public Res CmdStatus()
    {
        Console.WriteLine($"Current AfkService status is: {_afkService.GetCurrentStatus()}");
        return ResC.Ok();
    }

    [SubCommandModule(["start"], "Start AFK status")] 
    public Res CmdStart()
    {
        _logger.Debug("Manually starting AFK");
        _afkService.StartAfk();
        Console.WriteLine("Started AFK");
        return ResC.Ok();
    }

    [SubCommandModule(["stop"], "Stop AFK status")] 
    public Res CmdStop()
    {
        _logger.Debug("Manually stopping AFK");
        _afkService.StopAfk();
        Console.WriteLine("Stopped AFK");
        return ResC.Ok();
    }
}