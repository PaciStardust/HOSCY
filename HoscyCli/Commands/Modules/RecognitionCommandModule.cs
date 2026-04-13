using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Utility;

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
    public Res CmdSendText()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_ViaText));
    }

    [SubCommandModule(["send-audio"], "Should recognition be sent as audio")]
    public Res CmdSendAudio()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_ViaOther));
    }

    [SubCommandModule(["send-other"], "Should recognition be sent as other")]
    public Res CmdSendOther()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_ViaOther));
    }

    [SubCommandModule(["translate"], "Should recognition be translated")]
    public Res CmdDoTranslate()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_DoTranslate));
    }

    [SubCommandModule(["preprocess-partial"], "Should partial preprocessing be done")]
    public Res CmdPreprocessPartial()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_DoPreprocessPartial));
    }

    [SubCommandModule(["preprocess-full"], "Should full preprocessing be done")]
    public Res CmdPreprocessFull()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Send_DoPreprocessFull));
    }

    [SubCommandModule(["start-unmuted"], "Should recognizers start unmuted")]
    public Res CmdStartUnmuted()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Mute_StartUnmuted));
    }

    [SubCommandModule(["modules"], "Lists recognition modules")] 
    public Res CmdModules()
    {
        var modules = _recognition.GetModuleInfos();
        var moduleText = modules.Count > 0
            ? string.Join("\n", modules.Select(x => $" - {x.Name} > {x.Description}"))
            : "[NONE]";
        Console.WriteLine($"All available recognition modules:\n{moduleText}");
        return ResC.Ok();
    }

    [SubCommandModule(["selected-module"], "Module to use for recognition")]
    public Res CmdSelectedModule()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_SelectedModuleName));
    }

    [SubCommandModule(["fix-noise-filter"], "Manage noise filtering")]
    public Res CmdFixNoiseFilter()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Fixup_NoiseFilter));
        if (!res.IsOk) return res;

        return _recognition.UpdateSettings();
    }

    [SubCommandModule(["fix-remove-end-period"], "Removes period at the end of last sentence")]
    public Res CmdFixRemoveEndPeriod()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Fixup_RemoveEndPeriod));
    }

    [SubCommandModule(["fix-capitalize-first-letter"], "Capitalize first letter of result")]
    public Res CmdFixCapitalizeFirstLetter()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Fixup_CapitalizeFirstLetter));
    }
    #endregion
    
    #region Start / Stop
    [SubCommandModule(["status"], "Get the recognition status")]
    public Res CmdStatus()
    {
        var info = _recognition.GetCurrentModuleInfo();
        var infoText = info is null ? "None" : info.IsOk ? info.Value.Name : "ERROR";

        string[] textSplit = [
            $"Manager: {_recognition.GetCurrentStatus()}",
            $"Module ({infoText}): {_recognition.GetCurrentModuleStatus()}",
            $"Listening: {_recognition.IsListening}"
        ];
        var text = string.Join("\n", textSplit);
        Console.WriteLine(text);
        return ResC.Ok();
    }

    [SubCommandModule(["start"], "Start recognition module")]
    public Res CmdStart()
    {
        return _recognition.StartModule();
    }

    [SubCommandModule(["stop"], "Stop recognition module")]
    public Res CmdStop()
    {
        return _recognition.StopModule();
    }

    [SubCommandModule(["restart"], "Restart recognition module")]
    public Res CmdRestart()
    {
        return _recognition.RestartModule();
    }

    [SubCommandModule(["toggle-mute", "mute", "unmute"], "Toggle listening status of recognizer")]
    public Res CmdToggleMute()
    {
        var mode = !_recognition.IsListening;
        var result = _recognition.SetListening(mode);
        if (!result.IsOk) return ResC.Fail(result.Msg);

        Console.WriteLine($"Listening set to {result.Value} (requested={mode})");
        return ResC.Ok();
    }
    #endregion
}