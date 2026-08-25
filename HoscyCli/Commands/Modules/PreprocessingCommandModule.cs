using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Preprocessing;
using HoscyCore.Services.Output.Preprocessing.Replacements;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(PreprocessingCommandModule))]
public class PreprocessingCommandModule
(   
    ReflectPropEditCommandModule reflectCm,
    IPartialReplacementOutputPreprocessor preprocessPartial,
    IFullReplacementOutputPreprocessor preprocessFull
) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly IPartialReplacementOutputPreprocessor _preprocessPartial = preprocessPartial;
    private readonly IFullReplacementOutputPreprocessor _preprocessFull = preprocessFull;

    public string ModuleName => "Preprocessing";
    public string ModuleDescription => "Configure preprocessing";
    public string[] ModuleCommands => ["preprocessing"];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["do-replace-partial"], nameof(ConfigModel.Preprocessing_DoReplacementsPartial), ConfigModel.DESC_Preprocessing_DoReplacementsPartial),
            new(["do-replace-full"], nameof(ConfigModel.Preprocessing_DoReplacementsFull), ConfigModel.DESC_Preprocessing_DoReplacementsFull),
            new(["ignorechars-replace-full"], nameof(ConfigModel.Preprocessing_ReplacementFullIgnoredCharacters), ConfigModel.DESC_Preprocessing_ReplacementFullIgnoredCharacters)
        ];
    }

    [SubCommandModule(["edit-replace-partial"], ConfigModel.DESC_Preprocessing_ReplacementsPartial)]
    public Res CmdEditReplacePartial()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementsPartial));
        _preprocessPartial.ReloadReplacements();
        return res;
    }

    [SubCommandModule(["edit-replace-full"], ConfigModel.DESC_Preprocessing_ReplacementsFull)]
    public Res CmdEditReplaceFull()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementsFull));
        _preprocessFull.ReloadReplacements();
        return res;
    }
} 