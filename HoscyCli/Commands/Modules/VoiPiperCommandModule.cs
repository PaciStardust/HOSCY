using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(VoiPiperCommandModule))]
public class VoiPiperCommandModule
(   
    ReflectPropEditCommandModule reflectCm
) 
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "Voice - Piper";
    public string ModuleDescription => "Configure the piper voice module";
    public string[] ModuleCommands => [ "voi-piper" ];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["enabled"], nameof(ConfigModel.Voice_Piper_Process_Enabled), ConfigModel.DESC_Voice_Piper_Process_Enabled),

            new(["proc-terminal"], nameof(ConfigModel.Voice_Piper_Process_Terminal), ConfigModel.DESC_Voice_Piper_Process_Terminal),
            new(["proc-venv"], nameof(ConfigModel.Voice_Piper_Process_VenvDir), ConfigModel.DESC_Voice_Piper_Process_VenvDir),
            new(["proc-voice"], nameof(ConfigModel.Voice_Piper_Process_Voice), ConfigModel.DESC_Voice_Piper_Process_Voice),

            new(["ip"], nameof(ConfigModel.Voice_Piper_Ip), ConfigModel.DESC_Voice_Piper_Ip),
            new(["port"], nameof(ConfigModel.Voice_Piper_Port), ConfigModel.DESC_Voice_Piper_Port),

            new(["req-voice"], nameof(ConfigModel.Voice_Piper_Request_Voice), ConfigModel.DESC_Voice_Piper_Request_Voice),
            new(["req-noise-scale"], nameof(ConfigModel.Voice_Piper_Request_NoiseScale), ConfigModel.DESC_Voice_Piper_Request_NoiseScale),
            new(["req-noise-w-scale"], nameof(ConfigModel.Voice_Piper_Request_NoiseWScale), ConfigModel.DESC_Voice_Piper_Request_NoiseWScale)
        ];
    }
}