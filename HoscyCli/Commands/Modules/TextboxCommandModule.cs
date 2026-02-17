using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Handling.Textbox;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(TextboxCommandModule))]
public class TextboxCommandModule(IOutputManagerService output, VrcTextboxOutputHandlerStartInfo info, ReflectPropEditCommandModule reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly IOutputManagerService _output = output;
    private readonly VrcTextboxOutputHandlerStartInfo _info = info;
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName 
        => "Textbox";
    public string ModuleDescription 
        => "Configure the Vrc Textbox output module";
    public string[] ModuleCommands 
        => ["textbox"];

    [SubCommandModule(["status"], "Get output module status")]
    public CommandResult CmdStatus()
    {
        var status = _output.GetProcessorStatus(_info);
        Console.WriteLine($"Current status is {status}");
        return CommandResult.Success;
    }

    [SubCommandModule(["enabled"], "Enable VRC Textbox")] 
    public CommandResult CmdSetEnable()
    {
        var success = _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Enabled));
        _output.RefreshHandlers();
        return success;
    }

    [SubCommandModule(["trans-show"], "Show translation")] 
    public CommandResult CmdTransShow()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Output_ShowTranslation));
    }

    [SubCommandModule(["trans-add-original"], "Show both translation and original")] 
    public CommandResult CmdTransAddOriginal()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Output_AddOriginalToTranslation));
    }

    [SubCommandModule(["char-limit"], "Set content character limit")] 
    public CommandResult CmdCharLimit()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Output_MaxDisplayedCharacters));
    }

    [SubCommandModule(["do-output"], "Actually output text")] 
    public CommandResult CmdDoOutput()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Do_Output));
    }

    [SubCommandModule(["do-indicator"], "Actually show typing indicator")] 
    public CommandResult CmdDoIncidator()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Do_Indicator));
    }

    [SubCommandModule(["timeout-dyn-per20chars"], "Dynamic timeout in ms per 20 characters displayed")] 
    public CommandResult CmdTimeoutDynamic20Chars()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_DynamicPer20CharactersDisplayedMs));
    }

    [SubCommandModule(["timeout-dyn-min"], "Dynamic timeout minimum in ms")] 
    public CommandResult CmdTimeoutDynamicMin()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_DynamicMinimumMs));
    }

    [SubCommandModule(["timeout-dyn-use"], "Use dynamic timeout")] 
    public CommandResult CmdTimeoutDynamicUse()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_UseDynamic));
    }

    [SubCommandModule(["timeout-static"], "Dynamic timeout in ms")] 
    public CommandResult CmdTimeoutStatic()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_StaticMs));
    }

    [SubCommandModule(["clear-notif"], "Automatic clearing after notification")] 
    public CommandResult CmdClearNotification()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_AutomaticallyClearNotification));
    }

    [SubCommandModule(["clear-message"], "Automatic clearing after message")] 
    public CommandResult CmdClearMessage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Timeout_AutomaticallyClearMessage));
    }

    [SubCommandModule(["notif-text-start"], "Text at start of notification")] 
    public CommandResult CmdNotifTextStart()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Notification_IndicatorTextStart));
    }

    [SubCommandModule(["notif-text-end"], "Text at end of notification")] 
    public CommandResult CmdNotifTextEnd()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Notification_IndicatorTextEnd));
    }

    [SubCommandModule(["notif-priority"], "Use notification priority")] 
    public CommandResult CmdNotifPriority()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Notification_UsePrioritySystem));
    }

    [SubCommandModule(["notif-skip-on-message"], "Skip notification on message")] 
    public CommandResult CmdNotifSkipOnMessage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Notification_SkipWhenMessageAvailable));
    }

    [SubCommandModule(["sound-message"], "Play sound on message")] 
    public CommandResult CmdSoundMessage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Sound_OnMessage));
    }

    [SubCommandModule(["sound-notif"], "Play sound on notification")] 
    public CommandResult CmdSoundNotif()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.VrcTextbox_Sound_OnNotification));
    }
}