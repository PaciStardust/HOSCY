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

    [SubCommandModule(["models"], "Edit vosk model list")]
    public Res CmdModels()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Vosk_Models));
    }

    [SubCommandModule(["selected-model"], "Vosk model to use")]
    public Res CmdSelectedModel()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Vosk_CurrentModel));
    }

    [SubCommandModule(["new-word-wait-ms"], "Time to wait in MS for new word")]
    public Res CmdNewWordWait()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Vosk_NewWordWaitTimeMs));
    }
}