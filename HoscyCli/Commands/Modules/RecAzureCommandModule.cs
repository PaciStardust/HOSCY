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

    [SubCommandModule(["custom-endpoint"], "Custom endpoint to use")]
    public Res CmdCustomEndpoint()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Voice_Azure_CustomEndpoint));
    }

    [SubCommandModule(["preset-phrases"], "Phrases to add")]
    public Res CmdPresetPhrases()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Azure_PresetPhrases));
    }

    [SubCommandModule(["languages"], "Languages to detect")]
    public Res CmdLanguages()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Azure_Languages));
    }

    [SubCommandModule(["censor-profanity"], "Should profanity be censored")]
    public Res CmdCensorProfanity()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Azure_CensorProfanity));
    } //todo: [IMPL] To be implemented
}