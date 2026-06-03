using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(RecApiCommandModule))]
public class RecApiCommandModule
(
    ReflectPropEditCommandModule reflectCm
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "Recognition: API";
    public string ModuleDescription => "Configure the API Recognition module";
    public string[] ModuleCommands => ["rec-api"];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["selected-preset"], nameof(ConfigModel.Recognition_Api_Preset), ConfigModel.DESC_Recognition_Api_Preset),
            new(["max-recording-time"], nameof(ConfigModel.Recognition_Api_MaxRecordingTime), ConfigModel.DESC_Recognition_Api_MaxRecordingTime),
        ];
    }
}