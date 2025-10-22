using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Hoscy.Configuration.Modern;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Preprocessing;

/// <summary>
/// Handles replacing text fully
/// </summary>
public class FullReplacementOutputPreprocessor : IOutputPreprocessor
{
    private readonly ConfigModel _config;
    private readonly ILogger _logger;
    private readonly List<FullReplacementHandler> _handlers = [];

    public FullReplacementOutputPreprocessor(ConfigModel config, ILogger logger)
    {
        _config = config;
        ReloadFullReplacements();
        config.PropertyChanged += OnPropertyChanged;
        _logger = logger.ForContext<FullReplacementOutputPreprocessor>();
    }

    #region Info
    public OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Replace;

    public bool ShouldContinueIfHandled()
        => true;
    #endregion

    #region Reload
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) //todo: does this work?
    {
        if (e.PropertyName == nameof(_config.Speech_Replacement_Full))
        {
            ReloadFullReplacements();
        }
    }

    private void ReloadFullReplacements()
    {
        _logger.Information("Reloading {replacementModelCount} full replacements", _config.Speech_Replacement_Full.Count);
        _handlers.Clear();

        var converted = new List<FullReplacementHandler>();
        var countDisabled = 0;
        var countBroken = 0;

        foreach (var replacement in _config.Speech_Replacement_Full)
        {
            if (!replacement.Enabled)
            {
                countDisabled++;
                continue;
            }

            try
            {
                var handler = new FullReplacementHandler(replacement);
                converted.Add(handler);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Could not instantiate full replacement \"{replacementText}\", RegEx might be broken => Skipping and disabling", replacement.Text);
                replacement.Enabled = false;
                countBroken++;
            }
        }

        _handlers.AddRange(converted);
        _logger.Information("Reloaded {replacementModelCountLoaded}/{replacementModelCount} full replacements, {disabledCount} disabled, {brokenCount} broken", _handlers.Count, _config.Speech_Replacement_Full.Count, countDisabled, countBroken);
    }
    #endregion

    #region Processing
    public bool TryProcess(string input, [NotNullWhen(true)] out string? output)
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