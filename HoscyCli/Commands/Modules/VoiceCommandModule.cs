using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(VoiceCommandModule))]
public class VoiceCommandModule
(
    ReflectPropEditCommandModule reflectCm,
    IVoiceManagerService manager
)
    : AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly IVoiceManagerService _manager = manager;

    public string ModuleName => "Voice";
    public string ModuleDescription => "Configure voice synthesis";
    public string[] ModuleCommands => [ "voice", "synth" ];

    #region Config
    [SubCommandModule(["modules"], "Lists voice modules")] 
    public Res CmdModules()
    {
        var modules = _manager.GetModuleInfos();
        var moduleText = modules.Count > 0
            ? string.Join("\n", modules.Select(x => $" - {x.Name} > {x.Description}"))
            : "[NONE]";
        Console.WriteLine($"All available voice modules:\n{moduleText}");
        return ResC.Ok();
    }

    [SubCommandModule(["selected-module"], "Set module to use")]
    public Res CmdSelectedModule()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Voice_SelectedModuleName));
    }

    [SubCommandModule(["speaker"], "Name of speaker to use")]
    public Res CmdSpeaker()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Voice_CurrentSpeakerName));
    }

    [SubCommandModule(["autostart"], "Enable automatic start of module")]
    public Res CmdAutoStart()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Voice_AutoStart));
    }

    [SubCommandModule(["volume"], "Volume of played audio")]
    public Res CmdVolume()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Voice_AudioVolumePercent));
    }

    [SubCommandModule(["maximum-text-length"], "Maximum allowed text length")]
    public Res CmdMaxTextLength()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Translation_MaxTextLength));
    }

    [SubCommandModule(["skip-longer-text"], "Skip longer text instead of trimming it")]
    public Res CmdSkipLongerText()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Voice_SkipLongerText));
    }
    #endregion

    #region Start / Stop
    //todo: [FEAT] solomodulemanager command base?
    [SubCommandModule(["status"], "Get the voice status")]
    public Res CmdStatus()
    {
        var moduleInfo = _manager.GetCurrentModuleInfo();
        var text = $"Manager: {_manager.GetCurrentStatus()}\nModule ({(moduleInfo is null ? "None" : moduleInfo.IsOk ? moduleInfo.Value.Name : "ERROR")}): {_manager.GetCurrentModuleStatus()}";
        Console.WriteLine(text);
        return ResC.Ok();
    }

    [SubCommandModule(["start"], "Start voice module")]
    public Res CmdStart()
    {
        return _manager.StartModule();
    }

    [SubCommandModule(["stop"], "Stop voice module")]
    public Res CmdStop()
    {
        return _manager.StopModule();
    }

    [SubCommandModule(["restart"], "Restart voice module")]
    public Res CmdRestart()
    {
        return _manager.RestartModule();
    }
    #endregion
}