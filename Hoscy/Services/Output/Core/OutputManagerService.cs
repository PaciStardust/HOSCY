using System;
using System.Collections.Generic;
using System.Linq;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using Hoscy.Utility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Hoscy.Services.Output.Core;

[LoadIntoDiContainer(typeof(IOutputManagerService), Lifetime.Singleton)] //todo: check for inactive actives better
public class OutputManagerService(ILogger logger, IServiceProvider services, IBackToFrontNotifyService notify) : StartStopServiceBase, IOutputManagerService
{
    #region Injected
    private readonly ILogger _logger = logger.ForContext<OutputManagerService>();
    private readonly IServiceProvider _services = services;
    private readonly IBackToFrontNotifyService _notify = notify;
    #endregion

    #region Service Vars
    private readonly List<OutputProcessorInfo> _availableProcessors = [];
    private readonly List<IOutputProcessor> _activeProcessors = [];
    #endregion

    #region Events
    public event EventHandler<string> OnMessage = delegate { };
    public event EventHandler<OutputNotificationEventArgs> OnNotification = delegate {};
    public event EventHandler OnClear = delegate {};
    public event EventHandler<bool> OnProcessingIndicatorSet = delegate {};
    #endregion

    #region Start / Stop
    protected override void StartInternal()
    {
        _logger.Information("Starting up Service by loading available OutputProcessors");
        if (IsRunning())
        {
            _logger.Information("Skipped starting Service, still running");
            return;
        }

        _availableProcessors.Clear();
        var processorsWithInstance = LaunchUtils.GetImplementationsInContainerForClass<IOutputProcessor>(_services, _logger);
        _availableProcessors.AddRange(processorsWithInstance.Select(x => x.GetInfo()));
        if (_availableProcessors.Count == 0)
        {
            _logger.Warning("No Output Processors could be located, Service will have no functionality and will be NOT be marked as running");
            return;
        }
        _logger.Information("Started up Service with {processorCount} OutputProcessors", _availableProcessors.Count);
    }

    public override bool IsRunning()
    {
        return _availableProcessors.Count > 0;
    }

    public override void Stop()
    {
        var activeProcessorCount = _activeProcessors.Count;
        _logger.Information("Stopping service, shutting down {activeProcessors} Processors", activeProcessorCount);
        foreach (var processor in _activeProcessors)
        {
            ShutdownProcessor(processor.GetInfo());
        }

        var stillActiveProcessors = _activeProcessors.Where(x => x.GetStatus() != StartStopStatus.Stopped).ToArray();
        if (stillActiveProcessors.Length > 0)
        {
            var notStoppedProcessors = string.Join(", ", stillActiveProcessors.Select(x => x.GetType().FullName));
            throw new StartStopServiceException($"Following MessageProcessors failed to comply with a shutdown call: {notStoppedProcessors}");
        }
        _activeProcessors.Clear();
        _logger.Information("Stopped service, shut down {activeProcessors} Processors", activeProcessorCount);
    }

    public override bool TryRestart()
        => TryRestartSimple(GetType().Name, _logger, _notify);
    #endregion

    #region Info
    public IReadOnlyList<OutputProcessorInfo> GetInfos(bool activeOnly)
    {
        return activeOnly
            ? _activeProcessors.Where(x => x.GetStatus() != StartStopStatus.Stopped).Select(x => x.GetInfo()).ToList()
            : _availableProcessors;
    }
    #endregion

    #region Processor => Start / Stop
    public void ActivateProcessor(OutputProcessorInfo info) //todo: should all of these have trycatch
    {
        _logger.Information("Activating Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
        var activeMatch = RetrieveActiveProcessorWithInfo(info);
        if (activeMatch is not null)
        {
            _logger.Information("Terminating old Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
            ShutdownProcessor(info);
        }
        activeMatch = RetrieveActiveProcessorWithInfo(info);
        if (activeMatch is null)
        {
            _logger.Information("Terminated old Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
        }
        else
        {
            _logger.Error("Failed to terminate old Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
            throw new StartStopServiceException($"Unable to shut down Processor {info.ProcessorType.FullName}");
        }

        var newProcessor = RetrieveProcessorInstanceWithInfo(info);
        newProcessor.Activate();
        newProcessor.OnRuntimeError += HandleOnRuntimeError;
        _activeProcessors.Add(newProcessor);
        _logger.Information("Activated Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
    }

    public StartStopStatus GetProcessorStatus(OutputProcessorInfo info)
    {
        var activeProcessor = RetrieveActiveProcessorWithInfo(info);
        if (activeProcessor is null)
        {
            return StartStopStatus.Stopped;
        }
        if (activeProcessor.GetStatus() == StartStopStatus.Stopped)
        {
            _logger.Warning("GetProcessorStatus: Retrieved stopped Processor with name {processorName} and type {processorType} from active list",
                info.Name, info.ProcessorType.FullName);
        }
        return activeProcessor.GetStatus();
    }

    public void ShutdownProcessor(OutputProcessorInfo info)
    {
        _logger.Information("Shutting down Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
        var activeProcessor = RetrieveActiveProcessorWithInfo(info);
        if (activeProcessor is null)
        {
            _logger.Information("Processor with name {processorName} and type {processorType} is not active or does not exist", info.Name, info.GetType().FullName);
            return;
        }

        activeProcessor.Clear();
        activeProcessor.OnRuntimeError -= HandleOnRuntimeError;
        activeProcessor.Shutdown();
        _activeProcessors.Remove(activeProcessor); //todo: does that work?
        _logger.Information("Shut down Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
    }

    public void RestartProcessor(OutputProcessorInfo info)
    {
        _logger.Information("Restarting Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
        var activeProcessor = RetrieveActiveProcessorWithInfo(info);
        if (activeProcessor is null)
        {
            _logger.Information("Could not fine active Processor with name {processorName} and type {processorType}, starting instead", info.Name, info.GetType().FullName);
            ActivateProcessor(info);
        }
        else
        {
            activeProcessor.Restart();
        }
        _logger.Information("Restarted Processor with name {processorName} and type {processorType}", info.Name, info.GetType().FullName);
    }

    private void HandleOnRuntimeError(object? sender, Exception ex) //todo: is this the best way to solve this?
    {
        SetFaultLogAndNotify(ex, _logger, _notify, $"Encountered an error in Message Processor {sender?.GetType().FullName ?? "???"}");
    }

    private IOutputProcessor? RetrieveActiveProcessorWithInfo(OutputProcessorInfo info) //todo: warning when inactive?
    {
        var activeMatches = _activeProcessors.Where(x => x.GetInfo().ProcessorType == info.ProcessorType).ToArray();
        switch (activeMatches.Length)
        {
            case 0:
                return null;
            case 1:
                return activeMatches[0];
            default:
                _logger.Warning("Found multiple active {procCount} processors for InfoType {infoType}", activeMatches.Length, info.ProcessorType.FullName);
                return activeMatches[0];
        }
    }

    private IOutputProcessor RetrieveProcessorInstanceWithInfo(OutputProcessorInfo info)
    {
        var availableMatches = _availableProcessors.Where(x => x.ProcessorType == info.ProcessorType).ToArray();

        switch (availableMatches.Length)
        {
            case 0:
                _logger.Warning("Could not find any available processors for InfoType {infoType}", info.ProcessorType.FullName);
                throw new ArgumentException($"Could not find any available processors for InfoType {info.ProcessorType.FullName}");
            case 1:
                break;
            default:
                _logger.Warning("Found multiple {procCount} processors for InfoType {infoType}", availableMatches.Length, info.ProcessorType.FullName);
                break;
        }

        var searchMatch = _services.GetRequiredService(availableMatches[0].ProcessorType) as IOutputProcessor
            ?? throw new DiResolveException($"Unable to retrieve Processor {info.ProcessorType.FullName}");

        return searchMatch;
    }
    #endregion

    #region Processor => Control
    public bool SendMessage(string contents)
    {
        throw new NotImplementedException();
    }

    public bool SendNotification(string contents, OutputNotificationPriority priority)
    {
        throw new NotImplementedException();
    }

    public bool Clear()
    {
        throw new NotImplementedException();
    }

    public bool SetProcessingIndicator(bool isProcessing)
    {
        throw new NotImplementedException();
    }
    #endregion
}