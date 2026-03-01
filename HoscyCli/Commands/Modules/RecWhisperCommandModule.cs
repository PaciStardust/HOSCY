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

    [SubCommandModule(["cfg-increase-priority"], "Increase priority of Whisper process")]
    public CommandResult CmdCfgIncreasePriority()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_IncreaseThreadPriority));
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

    //todo: fix
    // [SubCommandModule(["cfg-language"], "Set recognized language")]
    // public CommandResult CmdCfgLanguage()
    // {
    //     return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_Language));
    // }

    [SubCommandModule(["cfg-thread-count"], "Number of threads to be used by process (0 = All, -N = All - N)")]
    public CommandResult CmdCfgThreadCount()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_ThreadsUsed));
    }

    [SubCommandModule(["cfg-max-context"], "Maximum context for recognition")]
    public CommandResult CmdCfgMaxContext()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_MaxContext));
    }

    [SubCommandModule(["cfg-max-segment-length"], "Maximum length of segments")]
    public CommandResult CmdCfgMaxSegmentLength()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_MaxSegmentLength));
    }

    [SubCommandModule(["cfg-max-sentence-duration"], "Maximum number of seconds for sentence")]
    public CommandResult CmdCfgMaxSentenceDuration()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_MaxSentenceDurationSeconds));
    }

    [SubCommandModule(["cfg-pause-detection-seconds"], "Duration to recognize a pause in seconds")]
    public CommandResult CmdCfgPauseDetectionSeconds()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_DetectPauseDurationSeconds));
    }

    [SubCommandModule(["list-graphics-adapters"], "Lists all graphics adapters (GPUs)")]
    public CommandResult CmdListGraphicAdapters()
    {
        // var gpus = _modelProvider.GetGraphicsAdapters();
        // var gpuText = gpus.Count > 0
        //     ? string.Join("\n", gpus.Select(x => $" - {x}"))
        //     : "[NONE]";
        // Console.WriteLine($"All available graphics adapters (GPUs):\n{gpuText}");
        // return CommandResult.Success;

        return CommandResult.Error; //todo: redo this all
    }

    [SubCommandModule(["cfg-graphics-adapter"], "Graphics adapter (GPU) to use")]
    public CommandResult CmdCfgGraphicsAdapter()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Recognition_Whisper_Cfg_GraphicsAdapter));
    }
}