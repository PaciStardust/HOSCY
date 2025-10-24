using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Preprocessing;

/// <summary>
/// Handles partial replacements of text
/// </summary>
[LoadIntoDiContainer(typeof(PartialReplacementOutputPreprocessor), Lifetime.Singleton)]
public class PartialReplacementOutputPreprocessor(ConfigModel config, ILogger logger) : ReplacementOutputPreprocessorBase<PartialReplacementHandler>(config, logger.ForContext<PartialReplacementOutputPreprocessor>())
{
    #region Simple Overrides
    public override PartialReplacementHandler ConvertToHandler(ReplacementDataModel model)
    {
        return new(model);
    }

    public override string GetReloadPropertyName()
        => nameof(_config.Speech_Replacement_Partial);

    public override ObservableCollection<ReplacementDataModel> GetReplacementModels()
        => _config.Speech_Replacement_Partial;

    public override OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Alter;

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