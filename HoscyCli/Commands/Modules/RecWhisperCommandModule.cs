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

    #region Model
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
    #endregion

    #region Debug
    [SubCommandModule(["dbg-log-filtered-noises"], "Write filtered noises into logs")]
    public CommandResult CmdDbgLogFilteredNoises()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Dbg_LogFilteredNoises));
    }
    #endregion

    #region Fix
    [SubCommandModule(["fix-random-brackets"], "Should random brackets be removed?")]
    public CommandResult CmdFixRandomBrackets()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Fix_RemoveRandomBrackets));
    }
    #endregion

    #region Cfg
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

    [SubCommandModule(["cfg-noise-filter"], "Edit noises to be filtered")]
    public CommandResult CmdCfgFilteredNoises()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_NoiseFilter));
    }

    [SubCommandModule(["cfg-use-gpu"], "Use the GPU for recognition")]
    public CommandResult CmdCfgUseGpu()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_UseGpu));
    }

    [SubCommandModule(["cfg-detect-language"], "Automatically detect spoken language")]
    public CommandResult CmdCfgDetectLanguage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_DetectLanguage));
    }

    [SubCommandModule(["cfg-language"], "Language code for recognition")]
    public CommandResult CmdCfgLanguage()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_Language));
    }

    [SubCommandModule(["cfg-max-sentence-duration-ms"], "Maximum duration of a sentence in MS")]
    public CommandResult CmdCfgMaxSentenceDurationMs()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_MaxSentenceDurationMs));
    }

    [SubCommandModule(["cfg-min-sentence-duration-ms"], "Minimum duration of a sentence in MS")]
    public CommandResult CmdCfgMinSentenceDurationMs()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_MinSentenceDurationMs));
    }

    [SubCommandModule(["cfg-detect-pause-duration-ms"], "Duration of a pause in MS")]
    public CommandResult CmdCfgDetectPauseDurationMs()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_DetectPauseDurationMs));
    }

    [SubCommandModule(["cfg-detect-outer-silence-duration-ms"], "Duration of outer silence in MS")]
    public CommandResult CmdCfgDetectOuterSilenceDurationMs()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_DetectOuterSilenceDurationMs));
    }

    [SubCommandModule(["cfg-recognition-update-interval-ms"], "How often should speech be sent to recognition?")]
    public CommandResult CmdCfgRecognitionUpdateIntervalMs()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_RecognitionUpdateIntervalMs));
    }

    [SubCommandModule(["cfg-vad-operating-mode"], "Operating mode for Voice Activity Detection")]
    public CommandResult CmdCfgVadOperatingMode()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_VadOperatingMode));
    }
    #endregion

    #region Cfg-Adv
    [SubCommandModule(["cfg-adv-thread-count"], "Number of threads to be used by process (0 = All, -N = All - N)")]
    public CommandResult CmdCfgAdvThreadCount()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_ThreadsUsed));
    }

    [SubCommandModule(["cfg-adv-max-segment-length"], "Maximum length of segments")]
    public CommandResult CmdCfgAdvMaxSegmentLength()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_MaxSegmentLength));
    }

    [SubCommandModule(["cfg-adv-beam-size"], "Beam size for beam search sampling strategy")]
    public CommandResult CmdCfgAdvBeamSize()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_BeamSize));
    }

    [SubCommandModule(["cfg-adv-greedy-best-of"], "Best of for greedy sampling strategy")]
    public CommandResult CmdCfgAdvGreedyBestOf()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_GreedyBestOf));
    }

    [SubCommandModule(["cfg-adv-gpu-id"], "ID of GPU to use")]
    public CommandResult CmdCfgAdvGpuId()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_GraphicsAdapterId));
    }

    [SubCommandModule(["cfg-adv-max-initial-t"], "MaxInitialT for Whisper")]
    public CommandResult CmdCfgAdvMaxInitialT()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_MaxInitialT));
    }

    [SubCommandModule(["cfg-adv-no-speech-threshold"], "No speech threshold for Whisper")]
    public CommandResult CmdCfgAdvNoSpeechThreshold()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_NoSpeechThreshold));
    }

    [SubCommandModule(["cfg-adv-temperature"], "Temperature for Whisper")]
    public CommandResult CmdCfgAdvTemperature()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_Temperature));
    }

    [SubCommandModule(["cfg-adv-temperature-inc"], "TemperatureInc for Whisper")]
    public CommandResult CmdCfgAdvTemperatureInc()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_TemperatureInc));
    }

    [SubCommandModule(["cfg-adv-max-tokens-per-segment"], "Max tokens per segment")]
    public CommandResult CmdCfgAdvMaxTokensPerSegment()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_MaxTokensPerSegment));
    }

    [SubCommandModule(["cfg-adv-prompt"], "Prompt for Whisper")]
    public CommandResult CmdCfgAdvPrompt()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_Prompt));
    }

    [SubCommandModule(["cfg-adv-set-threads"], "Should thread count be set")]
    public CommandResult CmdCfgAdvSetThreads()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_SetThreads));
    }

    [SubCommandModule(["cfg-adv-use-beam-search-sampling"], "Use beam search sampling strategy")]
    public CommandResult CmdCfgAdvUseBeamSearchSampling()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_UseBeamSearchSampling));
    }

    [SubCommandModule(["cfg-adv-use-greedy-sampling"], "Use greedy sampling strategy")]
    public CommandResult CmdCfgAdvUseGreedySampling()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_CfgAdv_UseGreedySampling));
    }
    #endregion
}