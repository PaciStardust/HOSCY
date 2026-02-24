using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Translation.Core;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(TranslationCommandModule))]
public class TranslationCommandModule
(   
    ReflectPropEditCommandModule _reflectCm,
    ITranslationManagerService translation
) 
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;
    private readonly ITranslationManagerService _translation = translation;

    public string ModuleName => "Translation";
    public string ModuleDescription => "Configure translation";
    public string[] ModuleCommands => [ "translation" ];

    #region Config
    [SubCommandModule(["modules"], "Lists translation modules")] 
    public CommandResult CmdModules()
    {
        var modules = _translation.GetModuleInfos();
        var moduleText = modules.Count > 0
            ? string.Join("\n", modules.Select(x => $" - {x.Name} > {x.Description}"))
            : "[NONE]";
        Console.WriteLine($"All available translation modules:\n{moduleText}");
        return CommandResult.Success;
    }

    [SubCommandModule(["selected-module", "module"], "Set module to use")]
    public CommandResult CmdSelectedModule()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_SelectedModuleName));
    }

    [SubCommandModule(["autostart"], "Should module be started on launch")]
    public CommandResult CmdAutostart()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_AutoStart));
    }

    [SubCommandModule(["skip-longer-messages"], "Should longer messages be skipped")]
    public CommandResult CmdSkipLongerMessages()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_SkipLongerMessages));
    }

    [SubCommandModule(["max-length"], "Maximum length of text to be translated")]
    public CommandResult CmdMaxLength()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_MaxTextLength));
    }

    [SubCommandModule(["untranslated-unavailable"], "Should untranslated content be sent if no translator available")]
    public CommandResult CmdUntranslatedUnavailable()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_SendUntranslatedIfUnavailable));
    }

    [SubCommandModule(["untranslated-failed"], "Should untranslated content be sent if translation failed")]
    public CommandResult CmdUntranslatedFailed()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_SendUntranslatedIfFailed));
    }
    #endregion

    #region Start / Stop
    [SubCommandModule(["status"], "Get the translator status")]
    public CommandResult CmdStatus()
    {
        var text = $"Manager: {_translation.GetCurrentStatus()}\nModule ({_translation.GetCurrentModuleInfo()?.Name ?? "None"}): {_translation.GetCurrentModuleStatus()}";
        Console.WriteLine(text);
        return CommandResult.Success;
    }

    [SubCommandModule(["start"], "Start translator module")]
    public CommandResult CmdStart()
    {
        var res = _translation.StartModule();
        return res ? CommandResult.Success : CommandResult.Error;
    }

    [SubCommandModule(["stop"], "Stop translator module")]
    public CommandResult CmdStop()
    {
        var res = _translation.StopModule();
        return res ? CommandResult.Success : CommandResult.Error;
    }

    [SubCommandModule(["restart"], "Restart translator module")]
    public CommandResult CmdRestart()
    {
        var res = _translation.RestartModule();
        return res ? CommandResult.Success : CommandResult.Error;
    }
    #endregion
}