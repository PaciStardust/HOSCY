using System;
using System.Collections.Generic;
using Hoscy.Services.DependencyCore;
using Serilog;

namespace Hoscy.Services.Output.Core;

[LoadIntoDiContainer(typeof(IOutputManagerService), Lifetime.Singleton)]
public class OutputManagerService(ILogger logger) : StartStopServiceBase, IOutputManagerService
{
    #region Injected
    private readonly ILogger _logger = logger.ForContext<OutputManagerService>();
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
        throw new NotImplementedException();
    }

    public override bool IsRunning()
    {
        throw new NotImplementedException();
    }

    public override void Stop()
    {
        throw new NotImplementedException();
    }

    public override bool TryRestart()
    {
        throw new NotImplementedException();
    }
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