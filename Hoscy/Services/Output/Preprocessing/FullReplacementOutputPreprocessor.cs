using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Logging;
using Hoscy.Configuration.Modern;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Preprocessing;


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
        _logger = logger;
    }

    public OutputPreprocessorHandlingStage GetHandlingStage()
    {
        throw new NotImplementedException();
    }

    public bool ShouldContinueIfHandled()
    {
        throw new NotImplementedException();
    }

    public bool TryProcess(string input, [NotNullWhen(true)] out string? output)
    {
        throw new NotImplementedException();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
}