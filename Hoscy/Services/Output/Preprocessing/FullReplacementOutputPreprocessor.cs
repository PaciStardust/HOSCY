using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Preprocessing;

/// <summary>
/// Handles replacing text fully
/// </summary>
[LoadIntoDiContainer(typeof(FullReplacementOutputPreprocessor), Lifetime.Singleton)]
public class FullReplacementOutputPreprocessor(ConfigModel config, ILogger logger) : ReplacementOutputPreprocessorBase<FullReplacementHandler>(config, logger.ForContext<FullReplacementOutputPreprocessor>())
{
    #region Simple Overrides
    public override FullReplacementHandler ConvertToHandler(ReplacementDataModel model)
        => new(model);

    public override string GetReloadPropertyName()
        => nameof(_config.Speech_Replacement_Full);

    public override ObservableCollection<ReplacementDataModel> GetReplacementModels()
        => _config.Speech_Replacement_Full;

    public override OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Replace;

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