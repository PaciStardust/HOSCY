using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(RecAzureCommandModule))]
public class RecAzureCommandModule(ReflectPropEditCommandModule reflectCm) 
    : AzureCommandModuleBase(reflectCm)
{
    public override string ModuleName 
        => "Recognition: Azure";
    public override string ModuleDescription 
        => "Configure the Azure Recognition module";
    public override string[] ModuleCommands
        => ["rec-azure"];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["custom-endpoint"], nameof(ConfigModel.Voice_Azure_CustomEndpoint), ConfigModel.DESC_Voice_Azure_CustomEndpoint),
            new(["preset-phrases"], nameof(ConfigModel.Recognition_Azure_PresetPhrases), ConfigModel.DESC_Recognition_Azure_PresetPhrases),
            new(["languages"], nameof(ConfigModel.Recognition_Azure_Languages), ConfigModel.DESC_Recognition_Azure_Languages),
            new(["censor-profanity"], nameof(ConfigModel.Recognition_Azure_CensorProfanity), ConfigModel.DESC_Recognition_Azure_CensorProfanity)
        ];
    }
}