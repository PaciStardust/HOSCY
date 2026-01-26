using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Output.Preprocessing;

/// <summary>
/// Handles replacing text fully
/// </summary>
[LoadIntoDiContainer(typeof(FullReplacementOutputPreprocessor), Lifetime.Transient)] //todo: [TEST] Write tests for this
public class FullReplacementOutputPreprocessor(ConfigModel config, ILogger logger) : ReplacementOutputPreprocessorBase<FullReplacementHandler>(config, logger.ForContext<FullReplacementOutputPreprocessor>())
{
    #region Simple Overrides
    public override FullReplacementHandler ConvertToHandler(ReplacementDataModel model)
    {
        return new(model);   
    }

    public override string GetReloadPropertyName()
        => nameof(_config.Preprocessing_ReplacementsFull);

    public override List<ReplacementDataModel> GetReplacementModels()
        => _config.Preprocessing_ReplacementsFull;

    public override OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Replace;

    public override bool IsFullReplace()
        => true;

    public override bool ShouldContinueIfHandled()
        => true;
    #endregion

    #region Processing
    public override bool TryProcess(string input, [NotNullWhen(true)] out string? output)
    {
        foreach (var handler in _handlers)
        {
            if (!handler.Compare(input)) continue;

            output = handler.GetReplacement();
            return true;
        }

        output = null;
        return false;
    }
    #endregion
}