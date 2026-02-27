using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Preprocessing;

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
    public CommandResult CmdDoReplacePartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_DoReplacementsPartial));
    }

    [SubCommandModule(["do-replace-full"], "Do full replacements")]
    public CommandResult CmdDoReplaceFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_DoReplacementsFull));
    }

    [SubCommandModule(["edit-replace-partial"], "Edit partial replacements")]
    public CommandResult CmdEditReplacePartial()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementsPartial));
        _preprocessPartial.ReloadReplacements();
        return res;
    }

    [SubCommandModule(["edit-replace-full"], "Edit full replacements")]
    public CommandResult CmdEditReplaceFull()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementsFull));
        _preprocessFull.ReloadReplacements();
        return res;
    }

    [SubCommandModule(["ignorechars-replace-full"], "Edit ignored characters for full replacements")]
    public CommandResult CmdIgnorecharsReplaceFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementFullIgnoredCharacters));
    }
} 