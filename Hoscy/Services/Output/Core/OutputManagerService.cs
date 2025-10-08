using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using Hoscy.Utility;
using Serilog;

namespace Hoscy.Services.Output.Core;

[LoadIntoDiContainer(typeof(IOutputManagerService), Lifetime.Singleton)]
public class OutputManagerService(ILogger logger, IServiceProvider services, IBackToFrontNotifyService notify) : StartStopServiceBase, IOutputManagerService
{
    #region Injected
    private readonly ILogger _logger = logger.ForContext<OutputManagerService>();
    private readonly IServiceProvider _services = services;
    private readonly IBackToFrontNotifyService _notify = notify;
    #endregion

    #region Service Vars
    private readonly List<OutputProcessorInfo> _availableProcessors = [];
    #endregion

    #region Events
    public event EventHandler<string> OnMessage = delegate {};
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
        throw new NotImplementedException();
    }

    public override bool TryRestart()
        => TryRestartSimple(GetType().Name, _logger, _notify);
    #endregion

    #region Info
    public IReadOnlyList<OutputProcessorInfo> GetInfos(bool activeOnly)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Processor => Start / Stop
    public void ActivateProcessor(OutputProcessorInfo info)
    {
        throw new NotImplementedException();
    }

    public bool IsProcessorActive(OutputProcessorInfo info)
    {
        throw new NotImplementedException();
    }

    public void ShutdownProcessor(OutputProcessorInfo info)
    {
        throw new NotImplementedException();
    }

    public void RestartProcessor(OutputProcessorInfo info)
    {
        throw new NotImplementedException();
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