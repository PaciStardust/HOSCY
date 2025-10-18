using System;
using Hoscy.Services.DependencyCore;
using Serilog;

namespace Hoscy.Services.Output.Core;

public abstract class OutputProcessorBase : IOutputProcessor
{
    #region Events
    public abstract event EventHandler<Exception> OnRuntimeError;
    public abstract event EventHandler OnShutdownCompleted;
    #endregion

    #region Info & Status
    public abstract OutputProcessorInfo GetInfo();

    public StartStopStatus GetStatus()
    {
        if (!IsRunning()) return StartStopStatus.Stopped;
        return GetFaultIfExists() is null ? StartStopStatus.Running : StartStopStatus.Faulted;
    }

    public abstract bool IsRunning();

    private Exception? _internalException = null;
    protected void SetFault(Exception? ex)
    {
        _internalException = ex;
    }

    public Exception? GetFaultIfExists()
    {
        return _internalException;
    }
    #endregion

    #region Start / Stop
    public void Activate()
    {
        SetFault(null);
        ActivateInternal();
    }
    protected abstract void ActivateInternal();
    public abstract void Restart();
    public void RestartSimple(string processorName, ILogger logger)
    {
        logger.Information("Restarting output processor {processorName}", processorName);
        Shutdown();
        Activate();
        logger.Information("Restarted output processor {processorName}", processorName);
    }
    public abstract void Shutdown();
    #endregion

    #region Functionality
    public abstract void Clear();
    public abstract void ProcessMessage(string contents);
    public abstract void ProcessNotification(string contents, OutputNotificationPriority priority);
    public abstract void SetProcessingIndicator(bool isProcessing);
    #endregion
}