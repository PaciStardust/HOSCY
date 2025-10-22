using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Hoscy.Configuration.Modern;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Preprocessing;

public class PartialReplacementOutputPreprocessor : IOutputPreprocessor
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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ReloadPartialReplacements()
    {
        throw new NotImplementedException();
    }

    public OutputPreprocessorHandlingStage GetHandlingStage()
    {
        throw new System.NotImplementedException();
    }

    public bool ShouldContinueIfHandled()
    {
        throw new System.NotImplementedException();
    }

    public bool TryProcess(string input, [NotNullWhen(true)] out string? output)
    {
        throw new System.NotImplementedException();
    }
}