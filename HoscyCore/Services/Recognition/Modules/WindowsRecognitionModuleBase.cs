using System.Runtime.Versioning;
using System.Speech.Recognition;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Recognition.Modules;

[SupportedOSPlatform("windows")]
public abstract class WindowsRecognitionModuleBase(ILogger logger, ConfigModel config, IRecognitionModelProviderService modelProvider) 
    : RecognitionModuleBase(logger)
{
    #region Vars
    protected readonly ConfigModel _config = config;
    private readonly IRecognitionModelProviderService _modelProvider = modelProvider;
    #endregion

    #region Functionality
    protected Res<SpeechRecognitionEngine> CreateEngine()
    {
        _logger.Debug("Creating new windows speech recognition engine");

        var recognizers = _modelProvider.GetWindowsRecognizers();
        if (!recognizers.IsOk) return ResC.TFail<SpeechRecognitionEngine>(recognizers.Msg);

        var recognizerInfo = recognizers.Value
            .Where(x => x.Id == _config.Recognition_Windows_ModelId)
            .ToArray();

        if (recognizerInfo.Length == 0)
        {
            _logger.Warning("Unable to instantiate engine with provided model id {modelId}, trying without", 
                _config.Recognition_Windows_ModelId);
            return ResC.TWrapR(() => new SpeechRecognitionEngine(), "Failed to create empty speech recognition engine", _logger);
        } else if (recognizerInfo.Length > 1)
        {
            _logger.Warning("Multiple matching infos found for model id {modelId}, picking first", _config.Recognition_Windows_ModelId);
        }

        return ResC.TWrapR(() => new SpeechRecognitionEngine(recognizerInfo[0].Id),
            $"Failed to create speech recognition engine for info {recognizerInfo[0].Name}", _logger);
    }

    protected void HandleSpeechDetected(object? sender, SpeechDetectedEventArgs e)
    {
        _logger.Verbose("Received speech activity");
        InvokeSpeechActivity(true);
    }

    protected void HandleSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        _logger.Verbose("Received speech recognized: \"{text}\"", e.Result.Text);
        InvokeSpeechActivity(false);
        InvokeSpeechRecognized(e.Result.Text);
    }
    #endregion
}