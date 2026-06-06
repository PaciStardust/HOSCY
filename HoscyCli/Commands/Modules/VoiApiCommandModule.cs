using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(VoiApiCommandModule))]
public class VoiApiCommandModule(ReflectPropEditCommandModule _reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "Voice: Api";
    public string ModuleDescription => "Configure the API voice module";
    public string[] ModuleCommands => [ "voi-api" ];

    [SubCommandModule(["preset"], ConfigModel.DESC_Voice_Api_Preset)]
    public Res CmdPreset()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Voice_Api_Preset));
    }
}