using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Preprocessing;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(PreprocessingCommandModule))]
public class PreprocessingCommandModule
(   
    ReflectPropEditCommandModule reflectCm,
    PartialReplacementOutputPreprocessor preprocessPartial,
    FullReplacementOutputPreprocessor preprocessFull
) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly PartialReplacementOutputPreprocessor _preprocessPartial = preprocessPartial;
    private readonly FullReplacementOutputPreprocessor _preprocessFull = preprocessFull;

    public string ModuleName => "Preprocessing";
    public string ModuleDescription => "Configure preprocessing";
    public string[] ModuleCommands => ["preprocessing"];

    [SubCommandModule(["do-replace-partial"], "Do partial replacements")]
    public Res CmdDoReplacePartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_DoReplacementsPartial));
    }

    [SubCommandModule(["do-replace-full"], "Do full replacements")]
    public Res CmdDoReplaceFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_DoReplacementsFull));
    }

    [SubCommandModule(["edit-replace-partial"], "Edit partial replacements")]
    public Res CmdEditReplacePartial()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementsPartial));
        _preprocessPartial.ReloadReplacements();
        return res;
    }

    [SubCommandModule(["edit-replace-full"], "Edit full replacements")]
    public Res CmdEditReplaceFull()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementsFull));
        _preprocessFull.ReloadReplacements();
        return res;
    }

    [SubCommandModule(["ignorechars-replace-full"], "Edit ignored characters for full replacements")]
    public Res CmdIgnorecharsReplaceFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementFullIgnoredCharacters));
    }
} 