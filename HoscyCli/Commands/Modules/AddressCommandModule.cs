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

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["game-afk", "afk"], nameof(ConfigModel.Osc_Address_Game_Afk), ConfigModel.DESC_Osc_Address_Game_Afk),
            new(["game-textbox", "textbox"], nameof(ConfigModel.Osc_Address_Game_Textbox), ConfigModel.DESC_Osc_Address_Game_Textbox),
            new(["game-typing", "typing"], nameof(ConfigModel.Osc_Address_Game_Typing), ConfigModel.DESC_Osc_Address_Game_Typing),
            new(["game-mute", "mute"], nameof(ConfigModel.Osc_Address_Game_Mute), ConfigModel.DESC_Osc_Address_Game_Mute),
            new(["in-message-text", "message-text"], nameof(ConfigModel.Osc_Address_Input_TextMessage), ConfigModel.DESC_Osc_Address_Input_TextMessage),
            new(["in-message-audio", "message-audio"], nameof(ConfigModel.Osc_Address_Input_AudioMessage), ConfigModel.DESC_Osc_Address_Input_AudioMessage),
            new(["in-message-other", "message-other"], nameof(ConfigModel.Osc_Address_Input_OtherMessage), ConfigModel.DESC_Osc_Address_Input_OtherMessage),
            new(["in-notification", "notification"], nameof(ConfigModel.Osc_Address_Input_TextNotification), ConfigModel.DESC_Osc_Address_Input_TextNotification),
            new(["rec-toggle-mute", "toggle-mute"], nameof(ConfigModel.Osc_Address_Tool_ToggleMute), ConfigModel.DESC_Osc_Address_Tool_ToggleMute),
            new(["rec-toggle-auto-mute", "toggle-auto-mute"], nameof(ConfigModel.Recognition_Mute_OnGameMute), ConfigModel.DESC_Recognition_Mute_OnGameMute),
            new(["media-pause"], nameof(ConfigModel.Osc_Address_Media_Pause), ConfigModel.DESC_Osc_Address_Media_Pause),
            new(["media-play"], nameof(ConfigModel.Osc_Address_Media_Play), ConfigModel.DESC_Osc_Address_Media_Play),
            new(["media-previous"], nameof(ConfigModel.Osc_Address_Media_Previous), ConfigModel.DESC_Osc_Address_Media_Previous),
            new(["media-next"], nameof(ConfigModel.Osc_Address_Media_Next), ConfigModel.DESC_Osc_Address_Media_Next),
            new(["media-toggle"],nameof(ConfigModel.Osc_Address_Media_Toggle), ConfigModel.DESC_Osc_Address_Media_Toggle),
            new(["toggle-replace-partial"],nameof(ConfigModel.Osc_Address_Tool_ToggleReplacementsPartial), ConfigModel.DESC_Osc_Address_Tool_ToggleReplacementsPartial),
            new(["toggle-replace-full"],nameof(ConfigModel.Osc_Address_Tool_ToggleReplacementsFull), ConfigModel.DESC_Osc_Address_Tool_ToggleReplacementsFull),
            new(["output-clear"],nameof(ConfigModel.Osc_Address_Tool_Clear), ConfigModel.DESC_Osc_Address_Tool_Clear)
        ];
    }
}