using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(VoiAzureCommandModule))]
public class VoiAzureCommandModule
(   
    ReflectPropEditCommandModule reflectCm
) 
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "Voice - Azure";
    public string ModuleDescription => "Configure the azure services voice module";
    public string[] ModuleCommands => [ "voi-azure" ];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["voices"], nameof(ConfigModel.Voice_Azure_VoiceList), ConfigModel.DESC_Voice_Azure_VoiceList),
            new(["selected-voice"], nameof(ConfigModel.Voice_Azure_CurrentVoice), ConfigModel.DESC_Voice_Azure_CurrentVoice),
            new(["custom-endpoint"], nameof(ConfigModel.Voice_Azure_CustomEndpoint), ConfigModel.DESC_Voice_Azure_CustomEndpoint)
        ];
    }
}