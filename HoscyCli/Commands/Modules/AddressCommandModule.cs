using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(AddressCommandModule))]
public class AddressCommandModule(ReflectPropEditCommandModule _reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "Address";
    public string ModuleDescription => "Configure osc addresses";
    public string[] ModuleCommands => [ "address", "osc-address" ];

    [SubCommandModule(["game-afk", "afk"], "Set games AFK address")] 
    public CommandResult CmdGameAfk()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Game_Afk));
    }

    [SubCommandModule(["game-textbox", "textbox"], "Set games textbox address")] 
    public CommandResult CmdGameTextbox()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Game_Textbox));
    }

    [SubCommandModule(["game-typing", "typing"], "Set games typing address")] 
    public CommandResult CmdGameTyping()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Game_Typing));
    }

    [SubCommandModule(["game-mute", "mute"], "Set games mute address")] 
    public CommandResult CmdGameMute()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Game_Mute));
    }

    [SubCommandModule(["in-message-text", "message-text"], "Set HOSCY text message input address")] 
    public CommandResult CmdInMessageText()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Input_TextMessage));
    }

    [SubCommandModule(["in-message-audio", "message-audio"], "Set HOSCY audio message input address")] 
    public CommandResult CmdInMessageAudio()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Input_AudioMessage));
    }

    [SubCommandModule(["in-message-other", "message-other"], "Set HOSCY other message input address")] 
    public CommandResult CmdInMessageOther()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Input_OtherMessage));
    }

    [SubCommandModule(["in-notification", "notification"], "Set HOSCY notification input address")] 
    public CommandResult CmdInNotification()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Input_TextNotification));
    }

    [SubCommandModule(["rec-toggle-mute", "toggle-mute"], "Set HOSCY recognition mute toggle address")] 
    public CommandResult CmdRecToggleMute()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Tool_ToggleMute));
    }

    [SubCommandModule(["rec-toggle-auto-mute", "toggle-auto-mute"], "Set HOSCY recognition automatic mute toggle address")] 
    public CommandResult CmdRecToggleAutoMute()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Mute_OnGameMute));
    }
}