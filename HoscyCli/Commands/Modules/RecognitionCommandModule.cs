using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(RecognitionCommandModule))]
public class RecognitionCommandModule
(
    ReflectPropEditCommandModule reflectCm,
    IRecognitionManagerService recognition
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly IRecognitionManagerService _recognition = recognition;

    public string ModuleName => "Recognition";
    public string ModuleDescription => "Configure Recognition";
    public string[] ModuleCommands => ["recognition"];

    #region Config
    [SubCommandModule(["send-text"], "Should recognition be sent as text")]
    public CommandResult CmdSendText()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_ViaText));
    }

    [SubCommandModule(["send-audio"], "Should recognition be sent as audio")]
    public CommandResult CmdSendAudio()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_ViaOther));
    }

    [SubCommandModule(["send-other"], "Should recognition be sent as other")]
    public CommandResult CmdSendOther()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_ViaOther));
    }

    [SubCommandModule(["send-translate"], "Should recognition be translated")]
    public CommandResult CmdSendTranslate()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_DoTranslate));
    }

    [SubCommandModule(["send-pre-partial"], "Should partial preprocessing be done")]
    public CommandResult CmdSendPrePartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_DoPreprocessPartial));
    }

    [SubCommandModule(["send-pre-full"], "Should full preprocessing be done")]
    public CommandResult CmdSendPreFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_DoPreprocessFull));
    }

    [SubCommandModule(["start-unmuted"], "Should recognizers start unmuted")]
    public CommandResult CmdStartUnmuted()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Mute_StartUnmuted));
    }

    [SubCommandModule(["modules"], "Lists recognition modules")] 
    public CommandResult CmdModules()
    {
        var modules = _recognition.GetModuleInfos();
        var moduleText = modules.Count > 0
            ? string.Join("\n", modules.Select(x => $" - {x.Name} > {x.Description}"))
            : "[NONE]";
        Console.WriteLine($"All available recognition modules:\n{moduleText}");
        return CommandResult.Success;
    }

    [SubCommandModule(["selected"], "Module to use for recognition")]
    public CommandResult CmdSelected()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_SelectedModuleName));
    }

    [SubCommandModule(["fix-noise-filter"], "Manage noise filtering")]
    public CommandResult CmdFixNoiseFilter()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Fixup_NoiseFilter));
        _recognition.UpdateSettings();
        return res;
    }

    [SubCommandModule(["fix-remove-end-period"], "Removes period at the end of last sentence")]
    public CommandResult CmdFixRemoveEndPeriod()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Fixup_RemoveEndPeriod));
    }

    [SubCommandModule(["fix-capitalize-first-letter"])]
    public CommandResult CmdFixCapitalizeFirstLetter()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Fixup_CapitalizeFirstLetter));
    }
    #endregion
    
    #region Start / Stop
    [SubCommandModule(["status"], "Get the recognition status")]
    public CommandResult CmdStatus()
    {
        var text = $"Manager: {_recognition.GetCurrentStatus()}\nModule ({_recognition.GetCurrentModuleInfo()?.Name ?? "None"}): {_recognition.GetCurrentModuleStatus()}";
        Console.WriteLine(text);
        return CommandResult.Success;
    }

    [SubCommandModule(["start"], "Start recognition module")]
    public CommandResult CmdStart()
    {
        var res = _recognition.StartModule();
        return res ? CommandResult.Success : CommandResult.Error;
    }

    [SubCommandModule(["stop"], "Stop recognition module")]
    public CommandResult CmdStop()
    {
        var res = _recognition.StopModule();
        return res ? CommandResult.Success : CommandResult.Error;
    }

    [SubCommandModule(["restart"], "Restart recognition module")]
    public CommandResult CmdRestart()
    {
        var res = _recognition.RestartModule();
        return res ? CommandResult.Success : CommandResult.Error;
    }
    #endregion
}