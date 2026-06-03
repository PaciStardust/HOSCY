using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(VoiceCommandModule))]
public class VoiceCommandModule
(
    ReflectPropEditCommandModule reflectCm,
    IVoiceManagerService manager
)
    : SoloModuleManagerCommandModuleBase<IVoiceManagerService, IVoiceModuleStartInfo>(reflectCm, manager), ICoreCommandModule
{
    public string ModuleName => "Voice";
    public string ModuleDescription => "Configure voice synthesis";
    public string[] ModuleCommands => [ "voice", "synth" ];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["selected-module"], nameof(ConfigModel.Voice_SelectedModuleName), ConfigModel.DESC_Voice_SelectedModuleName),
            new(["speaker"], nameof(ConfigModel.Voice_CurrentSpeakerName), ConfigModel.DESC_Voice_CurrentSpeakerName),
            new(["autostart"], nameof(ConfigModel.Voice_AutoStart), ConfigModel.DESC_Voice_AutoStart),
            new(["volume"], nameof(ConfigModel.Voice_AudioVolumePercent), ConfigModel.DESC_Voice_AudioVolumePercent),
            new(["maximum-text-length"], nameof(ConfigModel.Voice_MaximumTextLength), ConfigModel.DESC_Voice_MaximumTextLength),
            new(["skip-longer-text"], nameof(ConfigModel.Voice_SkipLongerText), ConfigModel.DESC_Voice_SkipLongerText)
        ];
    }
}