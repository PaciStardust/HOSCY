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

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["preset-message"], nameof(ConfigModel.Output_Api_Preset_Message), ConfigModel.DESC_Output_Api_Preset_Message),
            new(["preset-notification"], nameof(ConfigModel.Output_Api_Preset_Notification), ConfigModel.DESC_Output_Api_Preset_Notification),
            new(["preset-clear"], nameof(ConfigModel.Output_Api_Preset_Clear), ConfigModel.DESC_Output_Api_Preset_Clear),
            new(["preset-processing"], nameof(ConfigModel.Output_Api_Preset_Processing), ConfigModel.DESC_Output_Api_Preset_Processing),
            new(["value-true"], nameof(ConfigModel.Output_Api_Value_True), ConfigModel.DESC_Output_Api_Value_True),
            new(["value-false"], nameof(ConfigModel.Output_Api_Value_False), ConfigModel.DESC_Output_Api_Value_False),
            new(["trans-format"], nameof(ConfigModel.Output_Api_TranslationFormat), ConfigModel.DESC_Output_Api_TranslationFormat),
            new(["prepend-priority"], nameof(ConfigModel.Output_Api_PrependNotificationPriority), ConfigModel.DESC_Output_Api_PrependNotificationPriority),
        ];
    }

    [SubCommandModule(["status"], "Get output module status")]
    public Res CmdStatus()
    {
        var status = _output.GetProcessorStatus(_info);
        Console.WriteLine($"Current status is {status}");
        return ResC.Ok();
    }

    [SubCommandModule(["enabled"], ConfigModel.DESC_Output_Api_Enabled)] 
    public Res CmdSetEnable()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Output_Api_Enabled));
        if (!res.IsOk) return res;

        return _output.RefreshHandlers();
    }
}