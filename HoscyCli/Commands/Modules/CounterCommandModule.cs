using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(CounterCommandModule))]
public class CounterCommandModule(ReflectPropEditCommandModule _reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "Counters";
    public string ModuleDescription => "Configure counters";
    public string[] ModuleCommands => ["counters"];

    [SubCommandModule(["show"], "Show counter notifications")]
    public Res CmdShow()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Counters_ShowNotification));
    }

    [SubCommandModule(["edit", "list"], "Edit counters")]
    public Res CmdEdit()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Counters_List));
    }

    [SubCommandModule(["dsp-duration"], "How long counters should be displayed after change (in seconds)")]
    public Res CmdDspDuration()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Counters_DisplayDurationSeconds));
    }

    [SubCommandModule(["dsp-cooldown"], "How long between counter notifications (in seconds)")]
    public Res CmdDspCooldown()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Counters_DisplayCooldownSeconds));
    }
}