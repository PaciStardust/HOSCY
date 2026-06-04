using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Handlers;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(OutVoiceCommandModule))]
public class OutVoiceCommandModule
(
    IOutputManagerService output,
    VoiceOutputHandlerStartInfo info,
    ReflectPropEditCommandModule reflectCm
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly IOutputManagerService _output = output;
    private readonly VoiceOutputHandlerStartInfo _info = info;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName 
        => "Output: Voice";
    public string ModuleDescription 
        => "Configure the voice output module";
    public string[] ModuleCommands 
        => ["voice-api"];

    [SubCommandModule(["status"], "Get output module status")]
    public Res CmdStatus()
    {
        var status = _output.GetProcessorStatus(_info);
        Console.WriteLine($"Current status is {status}");
        return ResC.Ok();
    }

    [SubCommandModule(["enabled"], ConfigModel.DESC_Output_Voice_Enabled)] 
    public Res CmdSetEnable()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Output_Voice_Enabled));
        if (!res.IsOk) return res;

        return _output.RefreshHandlers();
    }

    [SubCommandModule(["send-translated"], ConfigModel.DESC_Output_Voice_SendTranslated)] 
    public Res CmdSendTranslated()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Output_Voice_SendTranslated));
    }
}