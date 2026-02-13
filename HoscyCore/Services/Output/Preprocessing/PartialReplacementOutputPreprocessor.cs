using System.Diagnostics.CodeAnalysis;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Output.Preprocessing;

/// <summary>
/// Handles partial replacements of text
/// </summary>
[LoadIntoDiContainer(typeof(PartialReplacementOutputPreprocessor), Lifetime.Transient)]
public class PartialReplacementOutputPreprocessor(ConfigModel config, ILogger logger) : ReplacementOutputPreprocessorBase<PartialReplacementHandler>(config, logger.ForContext<PartialReplacementOutputPreprocessor>())
{
    #region Simple Overrides
    public override bool IsEnabled()
        => _config.Preprocessing_DoReplacementsPartial;

    public override PartialReplacementHandler ConvertToHandler(ReplacementDataModel model)
    {
        return new(model);
    }

    public override List<ReplacementDataModel> GetReplacementModels()
        => _config.Preprocessing_ReplacementsPartial;

    public override OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Alter;

    public override bool IsFullReplace()
        => false;

    public override bool ShouldContinueIfHandled()
        => true;
    #endregion

    #region Processing
    public override bool TryProcess(string input, [NotNullWhen(true)] out string? output)
    {
        output = null;

        foreach (var handler in _handlers)
        {
            output = handler.Replace(output ?? input);
        }

        if (output is null || string.Equals(input, output, StringComparison.Ordinal))
        {
            output = null;
            return false;
        }
        
        return true;
    }
    #endregion
}