using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Misc;
using Serilog;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(AfkCommandModule))]
public class AfkCommandModule(IAfkService afkService, ILogger logger, ReflectPropEditCommandModule reflectCm) : AttributeCommandModule
{
    private readonly IAfkService _afkService = afkService;
    private readonly ILogger _logger = logger.ForContext<AfkCommandModule>();
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    [SubCommandModule(["status"], "Get service status")]
    public CommandResult CmdStatus()
    {
        Console.WriteLine($"Current AfkService status is: {_afkService.GetCurrentStatus()}");
        return CommandResult.Success;
    }

    [SubCommandModule(["start"], "Start AFK status")] 
    public CommandResult CmdStart()
    {
        _logger.Information("Manually starting AFK");
        _afkService.StartAfk();
        Console.WriteLine("Started AFK");
        return CommandResult.Success;
    }

    [SubCommandModule(["stop"], "Stop AFK status")] 
    public CommandResult CmdStop()
    {
        _logger.Information("Manually stopping AFK");
        _afkService.StopAfk();
        Console.WriteLine("Stopped AFK");
        return CommandResult.Success;
    }

    [SubCommandModule(["enabled"], "Enable AFK status")] 
    public CommandResult CmdSetEnable()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_ShowDuration));
    }

    [SubCommandModule(["interval"], "Set base AFK display interval (in seconds)")] 
    public CommandResult CmdSetInterval()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_BaseDurationDisplayIntervalSeconds));
    }

    [SubCommandModule(["double-time"], "Set times AFK is displayed before it is doubled")] 
    public CommandResult CmdSetDoubleTime()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_TimesDisplayedBeforeDoublingInterval));
    }

    [SubCommandModule(["txt-start"], "Set text to display when starting AFK")] 
    public CommandResult CmdSetTextStart()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_StartText));
    }

    [SubCommandModule(["txt-status"], "Set text to display during AFK")] 
    public CommandResult CmdSetTextStatus()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_StatusText));
    }

    [SubCommandModule(["txt-stop"], "Set text to display when stopping AFK")] 
    public CommandResult CmdSetTextStop()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Afk_StopText));
    }
}