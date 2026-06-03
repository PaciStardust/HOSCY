using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(RecWhisperCommandModule))]
public class RecWhisperCommandModule
(
    ReflectPropEditCommandModule reflectCm
)
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "Recognition: Whisper";
    public string ModuleDescription => "Configure the Whisper Recognition modules";
    public string[] ModuleCommands => ["rec-whisper"];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["models"], nameof(ConfigModel.Recognition_Whisper_Models), ConfigModel.DESC_Recognition_Whisper_Models),
            new(["selected-model"], nameof(ConfigModel.Recognition_Whisper_SelectedModel), ConfigModel.DESC_Recognition_Whisper_SelectedModel),

            new(["dbg-log-filtered-noises"], nameof(ConfigModel.Recognition_Whisper_Dbg_LogFilteredNoises), ConfigModel.DESC_Recognition_Whisper_Dbg_LogFilteredNoises),

            new(["fix-random-brackets"], nameof(ConfigModel.Recognition_Whisper_Fix_RemoveRandomBrackets), ConfigModel.DESC_Recognition_Whisper_Fix_RemoveRandomBrackets),
            
            new(["cfg-single-segment"], nameof(ConfigModel.Recognition_Whisper_Cfg_UseSingleSegmentMode), ConfigModel.DESC_Recognition_Whisper_Cfg_UseSingleSegmentMode),
            new(["cfg-translate-english"], nameof(ConfigModel.Recognition_Whisper_Cfg_TranslateToEnglish), ConfigModel.DESC_Recognition_Whisper_Cfg_TranslateToEnglish),
            new(["cfg-noise-filter"], nameof(ConfigModel.Recognition_Whisper_Cfg_NoiseFilter), ConfigModel.DESC_Recognition_Whisper_Cfg_NoiseFilter),
            new(["cfg-use-gpu"], nameof(ConfigModel.Recognition_Whisper_Cfg_UseGpu), ConfigModel.DESC_Recognition_Whisper_Cfg_UseGpu),
            new(["cfg-detect-language"], nameof(ConfigModel.Recognition_Whisper_Cfg_DetectLanguage), ConfigModel.DESC_Recognition_Whisper_Cfg_DetectLanguage),
            new(["cfg-language"], nameof(ConfigModel.Recognition_Whisper_Cfg_Language), ConfigModel.DESC_Recognition_Whisper_Cfg_Language),
            new(["cfg-max-sentence-duration-ms"], nameof(ConfigModel.Recognition_Whisper_Cfg_MaxSentenceDurationMs), ConfigModel.DESC_Recognition_Whisper_Cfg_MaxSentenceDurationMs),
            new(["cfg-min-sentence-duration-ms"], nameof(ConfigModel.Recognition_Whisper_Cfg_MinSentenceDurationMs), ConfigModel.DESC_Recognition_Whisper_Cfg_MinSentenceDurationMs),
            new(["cfg-detect-pause-duration-ms"], nameof(ConfigModel.Recognition_Whisper_Cfg_DetectPauseDurationMs), ConfigModel.DESC_Recognition_Whisper_Cfg_DetectPauseDurationMs),
            new(["cfg-detect-outer-silence-duration-ms"], nameof(ConfigModel.Recognition_Whisper_Cfg_DetectOuterSilenceDurationMs), ConfigModel.DESC_Recognition_Whisper_Cfg_DetectOuterSilenceDurationMs),
            new(["cfg-recognition-update-interval-ms"], nameof(ConfigModel.Recognition_Whisper_Cfg_RecognitionUpdateIntervalMs), ConfigModel.DESC_Recognition_Whisper_Cfg_RecognitionUpdateIntervalMs),
            new(["cfg-vad-operating-mode"], nameof(ConfigModel.Recognition_Whisper_Cfg_VadOperatingMode), ConfigModel.DESC_Recognition_Whisper_Cfg_VadOperatingMode),

            new(["cfg-adv-thread-count"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_ThreadsUsed), ConfigModel.DESC_Recognition_Whisper_CfgAdv_ThreadsUsed),
            new(["cfg-adv-max-segment-length"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_MaxSegmentLength), ConfigModel.DESC_Recognition_Whisper_CfgAdv_MaxSegmentLength),
            new(["cfg-adv-beam-size"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_BeamSize), ConfigModel.DESC_Recognition_Whisper_CfgAdv_BeamSize),
            new(["cfg-adv-greedy-best-of"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_GreedyBestOf), ConfigModel.DESC_Recognition_Whisper_CfgAdv_GreedyBestOf),
            new(["cfg-adv-gpu-id"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_GraphicsAdapterId), ConfigModel.DESC_Recognition_Whisper_CfgAdv_GraphicsAdapterId),
            new(["cfg-adv-max-initial-t"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_MaxInitialT), ConfigModel.DESC_Recognition_Whisper_CfgAdv_MaxInitialT),
            new(["cfg-adv-no-speech-threshold"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_NoSpeechThreshold), ConfigModel.DESC_Recognition_Whisper_CfgAdv_NoSpeechThreshold),
            new(["cfg-adv-temperature"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_Temperature), ConfigModel.DESC_Recognition_Whisper_CfgAdv_Temperature),
            new(["cfg-adv-temperature-inc"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_TemperatureInc), ConfigModel.DESC_Recognition_Whisper_CfgAdv_TemperatureInc),
            new(["cfg-adv-max-tokens-per-segment"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_MaxTokensPerSegment), ConfigModel.DESC_Recognition_Whisper_CfgAdv_MaxTokensPerSegment),
            new(["cfg-adv-prompt"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_Prompt), ConfigModel.DESC_Recognition_Whisper_CfgAdv_Prompt),
            new(["cfg-adv-set-threads"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_SetThreads), ConfigModel.DESC_Recognition_Whisper_CfgAdv_SetThreads),
            new(["cfg-adv-use-beam-search-sampling"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_UseBeamSearchSampling), ConfigModel.DESC_Recognition_Whisper_CfgAdv_UseBeamSearchSampling),
            new(["cfg-adv-use-greedy-sampling"], nameof(ConfigModel.Recognition_Whisper_CfgAdv_UseGreedySampling), ConfigModel.DESC_Recognition_Whisper_CfgAdv_UseGreedySampling),
        ];
    }
}