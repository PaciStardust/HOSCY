using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(PreprocessingCommandModule))]
public class PreprocessingCommandModule(ReflectPropEditCommandModule reflectCm) : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

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
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementsPartial));
    }

    [SubCommandModule(["edit-replace-full"], "Edit full replacements")]
    public CommandResult CmdEditReplaceFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementsFull));
    }

    [SubCommandModule(["ignorechars-replace-full"], "Edit ignored characters for full replacements")]
    public CommandResult CmdIgnorecharsReplaceFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Preprocessing_ReplacementFullIgnoredCharacters));
    }
} 