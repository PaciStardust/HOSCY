using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Handlers;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(TextboxCommandModule))]
public class TextboxCommandModule
(
    IOutputManagerService output,
    VrcTextboxOutputHandlerStartInfo info,
    ReflectPropEditCommandModule reflectCm
) 
: AttributeCommandModule, ICoreCommandModule
{
    private readonly IOutputManagerService _output = output;
    private readonly VrcTextboxOutputHandlerStartInfo _info = info;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName 
        => "Output: VRC Textbox";
    public string ModuleDescription 
        => "Configure the VRC Textbox output module";
    public string[] ModuleCommands 
        => ["out-textbox"];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["trans-show"], nameof(ConfigModel.Output_VrcTxt_Send_ShowTranslation), ConfigModel.DESC_Output_VrcTxt_Send_ShowTranslation),
            new(["trans-add-original"], nameof(ConfigModel.Output_VrcTxt_Send_AddOriginalToTranslation), ConfigModel.DESC_Output_VrcTxt_Send_AddOriginalToTranslation),
            new(["char-limit"], nameof(ConfigModel.Output_VrcTxt_Send_MaxDisplayedCharacters), ConfigModel.DESC_Output_VrcTxt_Send_MaxDisplayedCharacters),
            new(["do-output"], nameof(ConfigModel.Output_VrcTxt_Do_Send), ConfigModel.DESC_Output_VrcTxt_Do_Send),
            new(["do-indicator"], nameof(ConfigModel.Output_VrcTxt_Do_Indicator), ConfigModel.DESC_Output_VrcTxt_Do_Indicator),
            new(["timeout-dyn-per20chars"], nameof(ConfigModel.Output_VrcTxt_Timeout_DynamicPer20CharactersDisplayedMs), ConfigModel.DESC_Output_VrcTxt_Timeout_DynamicPer20CharactersDisplayedMs),
            new(["timeout-dyn-min"], nameof(ConfigModel.Output_VrcTxt_Timeout_DynamicMinimumMs), ConfigModel.DESC_Output_VrcTxt_Timeout_DynamicMinimumMs),
            new(["timeout-dyn-use"], nameof(ConfigModel.Output_VrcTxt_Timeout_UseDynamic), ConfigModel.DESC_Output_VrcTxt_Timeout_UseDynamic),
            new(["timeout-static"], nameof(ConfigModel.Output_VrcTxt_Timeout_StaticMs), ConfigModel.DESC_Output_VrcTxt_Timeout_StaticMs),
            new(["clear-notif"], nameof(ConfigModel.Output_VrcTxt_Timeout_AutomaticallyClearNotification), ConfigModel.DESC_Output_VrcTxt_Timeout_AutomaticallyClearNotification),
            new(["clear-message"], nameof(ConfigModel.Output_VrcTxt_Timeout_AutomaticallyClearMessage), ConfigModel.DESC_Output_VrcTxt_Timeout_AutomaticallyClearMessage),
            new(["notif-text-start"], nameof(ConfigModel.Output_VrcTxt_Notification_IndicatorTextStart), ConfigModel.DESC_Output_VrcTxt_Notification_IndicatorTextStart),
            new(["notif-text-end"], nameof(ConfigModel.Output_VrcTxt_Notification_IndicatorTextEnd), ConfigModel.DESC_Output_VrcTxt_Notification_IndicatorTextEnd),
            new(["notif-priority"], nameof(ConfigModel.Output_VrcTxt_Notification_UsePrioritySystem), ConfigModel.DESC_Output_VrcTxt_Notification_UsePrioritySystem),
            new(["notif-skip-on-message"], nameof(ConfigModel.Output_VrcTxt_Notification_SkipWhenMessageAvailable), ConfigModel.DESC_Output_VrcTxt_Notification_SkipWhenMessageAvailable),
            new(["sound-message"], nameof(ConfigModel.Output_VrcTxt_Sound_OnMessage), ConfigModel.DESC_Output_VrcTxt_Sound_OnMessage),
            new(["sound-notif"], nameof(ConfigModel.Output_VrcTxt_Sound_OnNotification), ConfigModel.DESC_Output_VrcTxt_Sound_OnNotification),
        ];
    }

    [SubCommandModule(["status"], "Get output module status")]
    public Res CmdStatus()
    {
        var status = _output.GetProcessorStatus(_info);
        Console.WriteLine($"Current status is {status}");
        return ResC.Ok();
    }

    [SubCommandModule(["enabled"], "Enable VRC Textbox")] 
    public Res CmdSetEnable()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Output_VrcTxt_Enabled));
        if (!res.IsOk) return res;

        return _output.RefreshHandlers();
    }
}