using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(TranslationCommandModule))]
public class TranslationCommandModule
(   
    ReflectPropEditCommandModule reflectCm,
    ITranslationManagerService manager
) 
: SoloModuleManagerCommandModuleBase<ITranslationManagerService, ITranslationModuleStartInfo>(reflectCm, manager), ICoreCommandModule
{
    public string ModuleName => "Translation";
    public string ModuleDescription => "Configure translation";
    public string[] ModuleCommands => [ "translation" ];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["selected-module"], nameof(ConfigModel.Translation_SelectedModuleName), ConfigModel.DESC_Translation_SelectedModuleName),
            new(["autostart"], nameof(ConfigModel.Translation_AutoStart), ConfigModel.DESC_Translation_AutoStart),
            new(["skip-longer-messages"], nameof(ConfigModel.Translation_SkipLongerMessages), ConfigModel.DESC_Translation_SkipLongerMessages),
            new(["max-length"], nameof(ConfigModel.Translation_MaxTextLength), ConfigModel.DESC_Translation_MaxTextLength),
            new(["untranslated-unavailable"], nameof(ConfigModel.Translation_SendUntranslatedIfUnavailable), ConfigModel.DESC_Translation_SendUntranslatedIfUnavailable),
            new(["untranslated-failed"], nameof(ConfigModel.Translation_SendUntranslatedIfFailed), ConfigModel.DESC_Translation_SendUntranslatedIfFailed)
        ];
    }
}