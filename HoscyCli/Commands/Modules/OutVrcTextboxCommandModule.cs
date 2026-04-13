using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Handlers;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(TextboxCommandModule))]
public class TextboxCommandModule
(
    IOutputManagerService output,
    VrcTextboxOutputHandlerStartInfo info,
    ReflectPropEditCommandModule reflectCm,
    IBackToFrontNotifyService notify
) 
: AttributeCommandModule, ICoreCommandModule
{
    private readonly IOutputManagerService _output = output;
    private readonly VrcTextboxOutputHandlerStartInfo _info = info;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly IBackToFrontNotifyService _notify = notify;

    public string ModuleName 
        => "Output: Textbox";
    public string ModuleDescription 
        => "Configure the Vrc Textbox output module";
    public string[] ModuleCommands 
        => ["out-textbox"];

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
        var res = _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Enabled));
        if (!res.IsOk) return res;

        return _output.RefreshHandlers();
    }

    [SubCommandModule(["trans-show"], "Show translation")] 
    public Res CmdTransShow()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Output_ShowTranslation));
    }

    [SubCommandModule(["trans-add-original"], "Show both translation and original")] 
    public Res CmdTransAddOriginal()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Output_AddOriginalToTranslation));
    }

    [SubCommandModule(["char-limit"], "Set content character limit")] 
    public Res CmdCharLimit()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Output_MaxDisplayedCharacters));
    }

    [SubCommandModule(["do-output"], "Actually output text")] 
    public Res CmdDoOutput()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Do_Output));
    }

    [SubCommandModule(["do-indicator"], "Actually show typing indicator")] 
    public Res CmdDoIncidator()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Do_Indicator));
    }

    [SubCommandModule(["timeout-dyn-per20chars"], "Dynamic timeout in ms per 20 characters displayed")] 
    public Res CmdTimeoutDynamic20Chars()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_DynamicPer20CharactersDisplayedMs));
    }

    [SubCommandModule(["timeout-dyn-min"], "Dynamic timeout minimum in ms")] 
    public Res CmdTimeoutDynamicMin()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_DynamicMinimumMs));
    }

    [SubCommandModule(["timeout-dyn-use"], "Use dynamic timeout")] 
    public Res CmdTimeoutDynamicUse()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_UseDynamic));
    }

    [SubCommandModule(["timeout-static"], "Dynamic timeout in ms")] 
    public Res CmdTimeoutStatic()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_StaticMs));
    }

    [SubCommandModule(["clear-notif"], "Automatic clearing after notification")] 
    public Res CmdClearNotification()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_AutomaticallyClearNotification));
    }

    [SubCommandModule(["clear-message"], "Automatic clearing after message")] 
    public Res CmdClearMessage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_AutomaticallyClearMessage));
    }

    [SubCommandModule(["notif-text-start"], "Text at start of notification")] 
    public Res CmdNotifTextStart()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Notification_IndicatorTextStart));
    }

    [SubCommandModule(["notif-text-end"], "Text at end of notification")] 
    public Res CmdNotifTextEnd()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Notification_IndicatorTextEnd));
    }

    [SubCommandModule(["notif-priority"], "Use notification priority")] 
    public Res CmdNotifPriority()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Notification_UsePrioritySystem));
    }

    [SubCommandModule(["notif-skip-on-message"], "Skip notification on message")] 
    public Res CmdNotifSkipOnMessage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Notification_SkipWhenMessageAvailable));
    }

    [SubCommandModule(["sound-message"], "Play sound on message")] 
    public Res CmdSoundMessage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Sound_OnMessage));
    }

    [SubCommandModule(["sound-notif"], "Play sound on notification")] 
    public Res CmdSoundNotif()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Sound_OnNotification));
    }
}