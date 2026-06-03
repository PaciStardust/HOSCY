using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(RecVoskCommandModule))]
public class RecVoskCommandModule
(
    ReflectPropEditCommandModule reflectCm
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "Recognition: Vosk";
    public string ModuleDescription => "Configure the Vosk Recognition module";
    public string[] ModuleCommands => ["rec-vosk"];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["models"], nameof(ConfigModel.Recognition_Vosk_Models), ConfigModel.DESC_Recognition_Vosk_Models),
            new(["selected-model"], nameof(ConfigModel.Recognition_Vosk_CurrentModel), ConfigModel.DESC_Recognition_Vosk_CurrentModel),
            new(["new-word-wait-ms"], nameof(ConfigModel.Recognition_Vosk_NewWordWaitTimeMs), ConfigModel.DESC_Recognition_Vosk_NewWordWaitTimeMs),
        ];
    }
}