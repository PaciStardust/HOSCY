using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(ApiCommandModule))]
public class ApiCommandModule(ReflectPropEditCommandModule reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "API";
    public string ModuleDescription => "Configure API settings";
    public string[] ModuleCommands => ["api"];

    [SubCommandModule(["presets"], "Configure API presets")] 
    public Res CmdPresets()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Api_Presets));
    }
}