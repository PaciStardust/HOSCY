using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(AddressCommandModule))]
public class AddressCommandModule(ReflectPropEditCommandModule _reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "Address";
    public string ModuleDescription => "Configure osc addresses";
    public string[] ModuleCommands => [ "address", "osc-address" ];

    [SubCommandModule(["game-afk", "afk"], "Set games AFK address")] 
    public Res CmdGameAfk()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Game_Afk));
    }

    [SubCommandModule(["game-textbox", "textbox"], "Set games textbox address")] 
    public Res CmdGameTextbox()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Game_Textbox));
    }

    [SubCommandModule(["game-typing", "typing"], "Set games typing address")] 
    public Res CmdGameTyping()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Game_Typing));
    }

    [SubCommandModule(["game-mute", "mute"], "Set games mute address")] 
    public Res CmdGameMute()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Game_Mute));
    }

    [SubCommandModule(["in-message-text", "message-text"], "Set HOSCY text message input address")] 
    public Res CmdInMessageText()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Input_TextMessage));
    }

    [SubCommandModule(["in-message-audio", "message-audio"], "Set HOSCY audio message input address")] 
    public Res CmdInMessageAudio()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Input_AudioMessage));
    }

    [SubCommandModule(["in-message-other", "message-other"], "Set HOSCY other message input address")] 
    public Res CmdInMessageOther()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Input_OtherMessage));
    }

    [SubCommandModule(["in-notification", "notification"], "Set HOSCY notification input address")] 
    public Res CmdInNotification()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Input_TextNotification));
    }

    [SubCommandModule(["rec-toggle-mute", "toggle-mute"], "Set HOSCY recognition mute toggle address")] 
    public Res CmdRecToggleMute()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Tool_ToggleMute));
    }

    [SubCommandModule(["rec-toggle-auto-mute", "toggle-auto-mute"], "Set HOSCY recognition automatic mute toggle address")] 
    public Res CmdRecToggleAutoMute()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Mute_OnGameMute));
    }

    [SubCommandModule(["media-pause"], "Set HOSCY media pause address")] 
    public Res CmdMediaPause()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Media_Pause));
    }

    [SubCommandModule(["media-play"], "Set HOSCY media play address")] 
    public Res CmdMediaPlay()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Media_Play));
    }

    [SubCommandModule(["media-previous"], "Set HOSCY media previous address")] 
    public Res CmdMediaPrevious()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Media_Previous));
    }

    [SubCommandModule(["media-next"], "Set HOSCY media next address")] 
    public Res CmdMediaNext()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Media_Next));
    }

    [SubCommandModule(["media-toggle"], "Set HOSCY media toggle address")] 
    public Res CmdMediaToggle()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Osc_Address_Media_Toggle));
    }
}