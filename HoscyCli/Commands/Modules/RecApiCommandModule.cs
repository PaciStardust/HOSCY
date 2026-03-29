using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;

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

    [SubCommandModule(["selected-preset"], "Preset to use")]
    public CommandResult CmdSelectedPreset()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Api_Preset));
    }

    [SubCommandModule(["max-recording-time"], "Maximum recording time")]
    public CommandResult CmdMaxRecordingTime()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Api_MaxRecordingTime));
    }
}