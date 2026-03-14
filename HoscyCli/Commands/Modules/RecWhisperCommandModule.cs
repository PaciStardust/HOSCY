using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Extra;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(RecWhisperCommandModule))]
public class RecWhisperCommandModule
(
    ReflectPropEditCommandModule reflectCm,
    IRecognitionModelProviderService modelProvider
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly IRecognitionModelProviderService _modelProvider = modelProvider;

    public string ModuleName => "Recognition: Whisper";
    public string ModuleDescription => "Configure the Whisper Recognition modules";
    public string[] ModuleCommands => ["rec-whisper"];

    [SubCommandModule(["models"], "Edit list of Whisper Models")]
    public CommandResult CmdModels()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Models));
    }

    [SubCommandModule(["selected-model"], "Set selected Whisper model")]
    public CommandResult CmdSelectedModel()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_SelectedModel));
    }

    [SubCommandModule(["cfg-single-segment"], "Should single segment mode be used?")]
    public CommandResult CmdCfgSingleSegmentMode()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_UseSingleSegmentMode));
    }

    [SubCommandModule(["cfg-translate-english"], "Should output be translated to English")]
    public CommandResult CmdCfgTranslateEnglish()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_TranslateToEnglish));
    }

    [SubCommandModule(["fix-random-brackets"], "Should random brackets be removed?")]
    public CommandResult CmdFixRandomBrackets()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Fix_RemoveRandomBrackets));
    }

    [SubCommandModule(["dbg-log-filtered-noises"], "Write filtered noises into logs")]
    public CommandResult CmdDbgLogFilteredNoises()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Dbg_LogFilteredNoises));
    }

    [SubCommandModule(["cfg-noise-filter"], "Edit noises to be filtered")]
    public CommandResult CmdCfgFilteredNoises()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_NoiseFilter));
    }

    [SubCommandModule(["cfg-thread-count"], "Number of threads to be used by process (0 = All, -N = All - N)")]
    public CommandResult CmdCfgThreadCount()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_ThreadsUsed));
    }

    [SubCommandModule(["cfg-max-segment-length"], "Maximum length of segments")]
    public CommandResult CmdCfgMaxSegmentLength()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_MaxSegmentLength));
    }
}