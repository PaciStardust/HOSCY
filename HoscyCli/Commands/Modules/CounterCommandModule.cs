using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(CounterCommandModule))]
public class CounterCommandModule(ReflectPropEditCommandModule _reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "Counters";
    public string ModuleDescription => "Configure counters";
    public string[] ModuleCommands => ["counters"];

    [SubCommandModule(["show"], "Show counter notifications")]
    public CommandResult CmdShow()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Counters_ShowNotification));
    }

    [SubCommandModule(["edit", "list"], "Edit counters")]
    public CommandResult CmdEdit()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Counters_List));
    }

    [SubCommandModule(["dsp-duration"], "How long counters should be displayed after change (in seconds)")]
    public CommandResult CmdDspDuration()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Counters_DisplayDurationSeconds));
    }

    [SubCommandModule(["dsp-cooldown"], "How long between counter notifications (in seconds)")]
    public CommandResult CmdDspCooldown()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Counters_DisplayCooldownSeconds));
    }
}