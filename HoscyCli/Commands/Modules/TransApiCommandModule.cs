using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(TransApiCommandModule))]
public class TransApiCommandModule(ReflectPropEditCommandModule _reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "Translation: Api";
    public string ModuleDescription => "Configure the API translation module";
    public string[] ModuleCommands => [ "trans-api" ];

    [SubCommandModule(["preset"], "Selected API preset for module")]
    public CommandResult CmdPreset()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_Api_Preset));
    }
}