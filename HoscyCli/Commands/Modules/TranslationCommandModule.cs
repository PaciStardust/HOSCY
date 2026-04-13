using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;

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
    public Res CmdModules()
    {
        var modules = _translation.GetModuleInfos();
        var moduleText = modules.Count > 0
            ? string.Join("\n", modules.Select(x => $" - {x.Name} > {x.Description}"))
            : "[NONE]";
        Console.WriteLine($"All available translation modules:\n{moduleText}");
        return ResC.Ok();
    }

    [SubCommandModule(["selected-module"], "Set module to use")]
    public Res CmdSelectedModule()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_SelectedModuleName));
    }

    [SubCommandModule(["autostart"], "Should module be started on launch")]
    public Res CmdAutostart()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_AutoStart));
    }

    [SubCommandModule(["skip-longer-messages"], "Should longer messages be skipped")]
    public Res CmdSkipLongerMessages()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_SkipLongerMessages));
    }

    [SubCommandModule(["max-length"], "Maximum length of text to be translated")]
    public Res CmdMaxLength()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_MaxTextLength));
    }

    [SubCommandModule(["untranslated-unavailable"], "Should untranslated content be sent if no translator available")]
    public Res CmdUntranslatedUnavailable()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_SendUntranslatedIfUnavailable));
    }

    [SubCommandModule(["untranslated-failed"], "Should untranslated content be sent if translation failed")]
    public Res CmdUntranslatedFailed()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_SendUntranslatedIfFailed));
    }
    #endregion

    #region Start / Stop
    [SubCommandModule(["status"], "Get the translation status")]
    public Res CmdStatus()
    {
        var moduleInfo = _translation.GetCurrentModuleInfo();
        var text = $"Manager: {_translation.GetCurrentStatus()}\nModule ({(moduleInfo is null ? "None" : moduleInfo.IsOk ? moduleInfo.Value.Name : "ERROR")}): {_translation.GetCurrentModuleStatus()}";
        Console.WriteLine(text);
        return ResC.Ok();
    }

    [SubCommandModule(["start"], "Start translation module")]
    public Res CmdStart()
    {
        return _translation.StartModule();
    }

    [SubCommandModule(["stop"], "Stop translation module")]
    public Res CmdStop()
    {
        return _translation.StopModule();
    }

    [SubCommandModule(["restart"], "Restart translation module")]
    public Res CmdRestart()
    {
        return _translation.RestartModule();
    }
    #endregion
}