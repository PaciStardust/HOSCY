using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using Hoscy.Utility;
using Serilog;

namespace Hoscy.Services.Translation.Core;

[LoadIntoDiContainer(typeof(ITranslatorManagerService), Lifetime.Singleton)]
public class TranslatorManagerService(IBackToFrontNotifyService notify, ILogger logger, IServiceProvider services) : StartStopServiceBase, ITranslatorManagerService
{
    #region Injected
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly ILogger _logger = logger.ForContext<TranslatorManagerService>();
    private readonly IServiceProvider _services = services;
    #endregion

    #region Service Vars
    private readonly List<(string, Type)> _availableTranslators = [];
    private ITranslator? _currentTranslator = null;
    #endregion

    #region Info
    /// <summary>
    /// Returns the name and type name of translators
    /// </summary>
    public IReadOnlyList<(string, string)> GetAvailableNames()
    {
        return _availableTranslators.Select(x => (x.Item1, x.Item2.Name)).ToList();
    }

    public string? GetCurrentName()
    {
        return _currentTranslator?.GetIdentifier();
    }
    #endregion

    #region Start / Stop
    protected override void StartInternal()
    {
        _logger.Information("Starting up Service by loading available Translators");
        if (IsRunning())
        {
            _logger.Information("Skipped starting Service, still running");
            return;
        }

        _availableTranslators.Clear();

        var translatorsWithInstance = LaunchUtils.GetImplementationsInContainerForClass<ITranslator>(_services, _logger);
        _availableTranslators.AddRange(translatorsWithInstance.Select(x => (x.GetIdentifier(), x.GetType())));
        if (_availableTranslators.Count == 0)
        {
            _logger.Warning("No Translators could be located, Service will have no functionality and will be NOT be marked as running");
            return;
        }

        _logger.Information("Started up Service with {translatorCount} Translators", _availableTranslators.Count);
    }

    public override void Stop()
    {
        _logger.Information("Stopping service, shutting down Translator");
        StopCurrentTranslator();
        _logger.Information("Stopped service, shut down Translator");
    }

    public override void Restart()
    {
        RestartSimple(GetType().Name, _logger);
    }

    public override bool IsRunning()
    {
        return _availableTranslators.Count > 0;
    }
    #endregion

    #region Translator => Start / Stop
    public void StartTranslator(string? name = null, string? typeName = null)
    {
        _logger.Information("Attemtping to start translator with name {name} and typeName {typeName}", name, typeName);
        if (_currentTranslator is not null)
        {
            _logger.Information("Skipped starting Translator, still running as {translatorType}", _currentTranslator.GetType().FullName);
            return;
        }

        if (name is null && typeName is null)
        {
            _logger.Error("Either name or typeName must be provided");
            throw new ArgumentException("Either name or typeName must be provided");
        }

        IEnumerable<(string, Type)> filteredTranslatorsEnumerable = _availableTranslators;
        if (name is not null)
            filteredTranslatorsEnumerable = filteredTranslatorsEnumerable.Where(x => x.Item1 == name);
        if (typeName is not null)
            filteredTranslatorsEnumerable = filteredTranslatorsEnumerable.Where(x => x.Item2.Name == typeName);

        var filteredTranslators = filteredTranslatorsEnumerable.ToArray();
        if (filteredTranslators.Length == 0)
        {
            _logger.Error("No translator found with the given name {name} or typeName {typeName}", name, typeName);
            throw new ArgumentException($"No translator found with the given name {name} or typeName {typeName}");
        }

        if (filteredTranslators.Length > 1)
        {
            var translatorOptions = string.Join(", ", filteredTranslators.Select(x => $"{x.Item1} ({x.Item2.Name})"));
            _logger.Warning("Multiple translators found with the given name {name} or typeName {typeName}: {translatorOptions} => picking first");
        }

        var translatorType = filteredTranslators[0].Item2;
        if (_services.GetService(translatorType) is not ITranslator translator)
        {
            _logger.Error("Failed to get translator instance name {translatorName} and type {translatorType}", name, translatorType.Name);
            throw new DiResolveException($"Failed to get translator instance name {name} and type {translatorType.Name}");
        }
        translator.OnRuntimeError += HandleOnRuntimeError;
        translator.OnSubmoduleStopped += HandleOnSubmoduleStopped;
        translator.Start();
        _currentTranslator = translator;
        _logger.Information("Started translator with name {translatorName} and type {translatorType}", name, translatorType.Name);
    }

    public void StopCurrentTranslator()
    {
        _logger.Information("Stopping current translator");
        if (_currentTranslator is null)
        {
            _logger.Information("Skipping stopping of current translator, no translator running");
            return;
        }
        _currentTranslator.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
        _currentTranslator.Stop();
        CleanupAfterTranslatorShutdown();
        _logger.Information("Stopped current translator");
    }

    private void HandleOnSubmoduleStopped(object? sender, EventArgs e)
    {
        if (sender is null) return;
        _logger.Warning("HandleOnShutdownCompleted called for type {senderType}, this should only happen when a shutdown was called unexpectedly", sender.GetType().FullName);
        CleanupAfterTranslatorShutdown();
    }

    private void CleanupAfterTranslatorShutdown()
    {
        if (_currentTranslator is not null)
        {
            _currentTranslator.OnRuntimeError -= HandleOnRuntimeError;
            _currentTranslator.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
            _currentTranslator = null;
        }
        SetFault(null);
    }

    private void HandleOnRuntimeError(object? sender, Exception ex)
    {
        _logger.Error(ex, "Encountered an error in Translator {senderType}", sender?.GetType().FullName);
        _notify.SendError($"Encountered an error in Translator {sender?.GetType().FullName ?? "???"}",exception: ex);
        var newEx = _currentTranslator?.GetFaultIfExists();
        SetFault(ex);
    }

    public void RestartCurrentTranslator()
    {
        _logger.Information("Restarting current translator");
        if (_currentTranslator is null)
        {
            _logger.Information("Skipping restart of current translator, no translator running");
            return;
        }
        _currentTranslator.OnSubmoduleStopped -= HandleOnSubmoduleStopped;
        _currentTranslator.Restart();
        _currentTranslator.OnSubmoduleStopped += HandleOnSubmoduleStopped;
        _logger.Information("Restarted current translator");
    }

    public StartStopStatus GetCurrentTranslatorStatus()
    {
        return _currentTranslator?.GetStatus() ?? StartStopStatus.Stopped;
    }
    #endregion

    #region Translator => Functionality
    public bool TryTranslate(string input, [NotNullWhen(true)] out string? output)
    {
        if (_currentTranslator is null)
        {
            _logger.Warning("Skipped translation request for input \"{input}\", no translator running", input);
            output = null;
            return false;
        }

        return _currentTranslator.TryTranslate(input, out output);
    }
    #endregion
}