using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Handlers;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(OutApiCommandModule))]
public class OutApiCommandModule
(
    IOutputManagerService output,
    ApiOutputHandlerStartInfo info,
    ReflectPropEditCommandModule reflectCm
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly IOutputManagerService _output = output;
    private readonly ApiOutputHandlerStartInfo _info = info;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName 
        => "Output: API";
    public string ModuleDescription 
        => "Configure the API output module";
    public string[] ModuleCommands 
        => ["out-api"];

    [SubCommandModule(["status"], "Get output module status")]
    public Res CmdStatus()
    {
        var status = _output.GetProcessorStatus(_info);
        Console.WriteLine($"Current status is {status}");
        return ResC.Ok();
    }

    [SubCommandModule(["enabled"], "Enable API Output")] 
    public Res CmdSetEnable()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.ApiOut_Enabled));
        if (!res.IsOk) return res;

        return _output.RefreshHandlers();
    }

    [SubCommandModule(["preset-message"], "Set API preset for message")] 
    public Res CmdPresetMessage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ApiOut_Preset_Message));
    }

    [SubCommandModule(["preset-notification"], "Set API preset for notification")] 
    public Res CmdPresetNotification()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ApiOut_Preset_Notification));
    }

    [SubCommandModule(["preset-clear"], "Set API preset for clearing")] 
    public Res CmdPresetClear()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ApiOut_Preset_Clear));
    }

    [SubCommandModule(["preset-processing"], "Set API preset for processing indicator")] 
    public Res CmdPresetProcessing()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ApiOut_Preset_Processing));
    }

    [SubCommandModule(["value-true"], "Set API value for TRUE")] 
    public Res CmdValueTrue()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ApiOut_Value_True));
    }

    [SubCommandModule(["value-false"], "Set API value for FALSE")] 
    public Res CmdValueFalse()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ApiOut_Value_False));
    }

    [SubCommandModule(["trans-format"], "Set translation format")] 
    public Res CmdTransFormat()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.ApiOut_TranslationFormat));
    }
}