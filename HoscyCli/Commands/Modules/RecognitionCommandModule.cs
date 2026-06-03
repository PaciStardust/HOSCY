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
: SoloModuleManagerCommandModuleBase<IRecognitionManagerService, IRecognitionModuleStartInfo>(reflectCm, recognition), ICoreCommandModule
{
    public string ModuleName => "Recognition";
    public string ModuleDescription => "Configure Recognition";
    public string[] ModuleCommands => ["recognition"];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["send-text"], nameof(ConfigModel.Recognition_Send_ViaText), ConfigModel.DESC_Recognition_Send_ViaText),
            new(["send-audio"], nameof(ConfigModel.Recognition_Send_ViaAudio), ConfigModel.DESC_Recognition_Send_ViaAudio),
            new(["send-other"], nameof(ConfigModel.Recognition_Send_ViaOther), ConfigModel.DESC_Recognition_Send_ViaOther),
            new(["translate"], nameof(ConfigModel.Recognition_Send_DoTranslate), ConfigModel.DESC_Recognition_Send_DoTranslate),
            new(["preprocess-partial"], nameof(ConfigModel.Recognition_Send_DoPreprocessPartial), ConfigModel.DESC_Recognition_Send_DoPreprocessPartial),
            new(["preprocess-full"], nameof(ConfigModel.Recognition_Send_DoPreprocessFull), ConfigModel.DESC_Recognition_Send_DoPreprocessFull),
            new(["start-unmuted"], nameof(ConfigModel.Recognition_Mute_StartUnmuted), ConfigModel.DESC_Recognition_Mute_StartUnmuted),
            new(["selected-module"], nameof(ConfigModel.Recognition_SelectedModuleName), ConfigModel.DESC_Recognition_SelectedModuleName),
            new(["fix-remove-end-period"], nameof(ConfigModel.Recognition_Fixup_RemoveEndPeriod), ConfigModel.DESC_Recognition_Fixup_RemoveEndPeriod),
            new(["fix-capitalize-first-letter"], nameof(ConfigModel.Recognition_Fixup_CapitalizeFirstLetter), ConfigModel.DESC_Recognition_Fixup_CapitalizeFirstLetter),
            new(["microphone"], nameof(ConfigModel.Recognition_MicrophoneName), ConfigModel.DESC_Recognition_MicrophoneName),
            new(["autostart"], nameof(ConfigModel.Recognition_AutoStart), ConfigModel.DESC_Recognition_AutoStart)
        ];
    }
    
    #region Control
    [SubCommandModule(["fix-noise-filter"], ConfigModel.DESC_Recognition_Fixup_NoiseFilter)]
    public Res CmdFixNoiseFilter()
    {
        var res = _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Fixup_NoiseFilter));
        if (!res.IsOk) return res;

        return _manager.UpdateSettings();
    }

    protected override string[] GetAdditionalStatusLines()
    {
        return [$"Listening: {_manager.IsListening}"];
    }

    [SubCommandModule(["toggle-mute", "mute", "unmute"], "Toggle listening status of recognizer")]
    public Res CmdToggleMute()
    {
        var mode = !_manager.IsListening;
        var result = _manager.SetListening(mode);
        if (!result.IsOk) return ResC.Fail(result.Msg);

        Console.WriteLine($"Listening set to {result.Value} (requested={mode})");
        return ResC.Ok();
    }
    #endregion
}