using System.Diagnostics.CodeAnalysis;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Output.Preprocessing;

/// <summary>
/// Handles replacing text fully
/// </summary>
[LoadIntoDiContainer(typeof(FullReplacementOutputPreprocessor), Lifetime.Transient)]
public class FullReplacementOutputPreprocessor(ConfigModel config, ILogger logger) 
    : ReplacementOutputPreprocessorBase<FullReplacementHandler>(config, logger.ForContext<FullReplacementOutputPreprocessor>())
{
    #region Simple Overrides
    public override bool IsEnabled()
        => _config.Preprocessing_DoReplacementsFull;

    public override FullReplacementHandler ConvertToHandler(ReplacementDataModel model)
    {
        return new(model);   
    }

    public override List<ReplacementDataModel> GetReplacementModels()
        => _config.Preprocessing_ReplacementsFull;

    public override OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Replace;

    public override bool IsFullReplace()
        => true;
    #endregion

    #region Processing
    public override OutputPreprocessorResult Process(ref string contents)
    {
        var input = string.Concat(contents.Where(x => !_config.Preprocessing_ReplacementFullIgnoredCharacters.Contains(x)));
        foreach (var handler in _handlers)
        {
            if (!handler.Compare(input)) continue;

            contents = handler.GetReplacement();
            return OutputPreprocessorResult.ProcessedContinue;
        }

        return OutputPreprocessorResult.NotProcessed;
    }
    #endregion
}