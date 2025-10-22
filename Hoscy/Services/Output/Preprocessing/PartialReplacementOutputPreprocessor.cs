using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Preprocessing;

[LoadIntoDiContainer(typeof(FullReplacementOutputPreprocessor), Lifetime.Singleton)]
/// <summary>
/// Handles partial replacements of text
/// </summary>
public class PartialReplacementOutputPreprocessor : IOutputPreprocessor //todo: base class?
{
    private readonly ConfigModel _config;
    private readonly ILogger _logger;
    private readonly List<PartialReplacementHandler> _handlers = [];

    public PartialReplacementOutputPreprocessor(ConfigModel config, ILogger logger)
    {
        _config = config;
        ReloadPartialReplacements();
        config.PropertyChanged += OnPropertyChanged;
        _logger = logger.ForContext<PartialReplacementOutputPreprocessor>();
    }

    #region Info
    public OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Alter;

    public bool ShouldContinueIfHandled()
        => true;
    #endregion


    #region Reload
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) //todo: does this work?
    {
        if (e.PropertyName == nameof(_config.Speech_Replacement_Partial))
        {
            ReloadPartialReplacements();
        }
    }

    private void ReloadPartialReplacements()
    {
        _logger.Information("Reloading {replacementModelCount} partial replacements", _config.Speech_Replacement_Partial.Count);
        _handlers.Clear();

        var converted = new List<PartialReplacementHandler>();
        var countDisabled = 0;
        var countBroken = 0;

        foreach (var replacement in _config.Speech_Replacement_Partial)
        {
            if (!replacement.Enabled)
            {
                countDisabled++;
                continue;
            }

            try
            {
                var handler = new PartialReplacementHandler(replacement);
                converted.Add(handler);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Could not instantiate partial replacement \"{replacementText}\", RegEx might be broken => Skipping and disabling", replacement.Text);
                replacement.Enabled = false;
                countBroken++;
            }
        }

        _handlers.AddRange(converted);
        _logger.Information("Reloaded {replacementModelCountLoaded}/{replacementModelCount} patrial replacements, {disabledCount} disabled, {brokenCount} broken", _handlers.Count, _config.Speech_Replacement_Partial.Count, countDisabled, countBroken);
    }
    #endregion

    public bool TryProcess(string input, [NotNullWhen(true)] out string? output)
    {
        throw new System.NotImplementedException();
    }
}