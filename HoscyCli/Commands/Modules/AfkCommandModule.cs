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

    [SubCommandModule(["enabled"], "Enable AFK status")] 
    public Res CmdSetEnable()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_ShowDuration));
    }

    [SubCommandModule(["interval"], "Set base AFK display interval (in seconds)")] 
    public Res CmdSetInterval()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_BaseDurationDisplayIntervalSeconds));
    }

    [SubCommandModule(["double-time"], "Set times AFK is displayed before it is doubled")] 
    public Res CmdSetDoubleTime()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_TimesDisplayedBeforeDoublingInterval));
    }

    [SubCommandModule(["txt-start"], "Set text to display when starting AFK")] 
    public Res CmdSetTextStart()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_StartText));
    }

    [SubCommandModule(["txt-status"], "Set text to display during AFK")] 
    public Res CmdSetTextStatus()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_StatusText));
    }

    [SubCommandModule(["txt-stop"], "Set text to display when stopping AFK")] 
    public Res CmdSetTextStop()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_StopText));
    }
}