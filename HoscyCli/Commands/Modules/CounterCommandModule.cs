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

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["show"], nameof(ConfigModel.Counters_ShowNotification), ConfigModel.DESC_Counters_ShowNotification),
            new(["edit", "list"], nameof(ConfigModel.Counters_List), ConfigModel.DESC_Counters_List),
            new(["dsp-duration"], nameof(ConfigModel.Counters_DisplayDurationSeconds), ConfigModel.DESC_Counters_DisplayDurationSeconds),
            new(["dsp-cooldown"], nameof(ConfigModel.Counters_DisplayCooldownSeconds), ConfigModel.DESC_Counters_DisplayCooldownSeconds)
        ];
    }
}