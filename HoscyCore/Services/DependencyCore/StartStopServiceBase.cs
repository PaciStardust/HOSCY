using HoscyCore.Services.Interfacing;
using Serilog;

namespace HoscyCore.Services.DependencyCore;

/// <summary>
/// Helper class for ease of use for the StartStopServices
/// </summary>
public abstract class StartStopServiceBase : IStartStopService
{
    private Exception? _internalException = null;

    public void Start()
    {
        SetFault(null);
        StartInternal();
    }
    protected abstract void StartInternal();

    public abstract void Stop();
    public abstract void Restart();
    public Exception? GetFaultIfExists()
        => _internalException;

    protected virtual void SetFault(Exception? ex)
    {
        _internalException = ex;
    }

    public ServiceStatus GetCurrentStatus()
    {
        if (!IsStarted()) return ServiceStatus.Stopped;
        if (GetFaultIfExists() is not null) return ServiceStatus.Faulted;
        if (IsProcessing()) return ServiceStatus.Processing;
        return ServiceStatus.Started;
    }

    protected abstract bool IsStarted();
    protected abstract bool IsProcessing();

    protected void RestartSimple(Type logType, ILogger logger)
    {
        LogRestartBegin(logType, logger);
        Stop();
        Start();
        LogRestartComplete(logType, logger);
    }

    protected void SetFaultLogAndNotify(Exception ex, ILogger? logger, IBackToFrontNotifyService? notify, string message)
    {
        SetFault(ex);
        logger?.Error(ex, message);
        notify?.SendError(message, exception: ex);
    }

    #region Standard Log
    protected void LogStartBegin(Type logType, ILogger logger)
    {
        logger.Debug("{serviceName}: Service starting", logType.Name);
    }

    protected void LogStartAlreadyStarted(Type logType, ILogger logger)
    {
        logger.Debug("{serviceName}: Service start cancelled, already started", logType.Name);
    }

    protected void LogStartComplete(Type logType, ILogger logger)
    {
        logger.Debug("{serviceName}: Service started", logType.Name);
    }

    protected void LogRestartBegin(Type logType, ILogger logger)
    {
        logger.Debug("{serviceName}: Service restarting", logType.Name);
    }

    protected void LogRestartComplete(Type logType, ILogger logger)
    {
        logger.Debug("{serviceName}: Service restarted", logType.Name);
    }

    protected void LogStopBegin(Type logType, ILogger logger)
    {
        logger.Debug("{serviceName}: Service stopping", logType.Name);
    }

    protected void LogStopComplete(Type logType, ILogger logger)
    {
        logger.Debug("{serviceName}: Service stopped", logType.Name);
    }
    #endregion
}