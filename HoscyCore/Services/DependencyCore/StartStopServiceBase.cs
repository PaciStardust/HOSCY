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
    public abstract bool IsRunning();

    public Exception? GetFaultIfExists()
        => _internalException;

    protected virtual void SetFault(Exception? ex)
    {
        _internalException = ex;
    }

    public StartStopStatus GetStatus()
    {
        if (!IsRunning()) return StartStopStatus.Stopped;
        if (GetFaultIfExists() is not null) return StartStopStatus.Faulted;
        return StartStopStatus.Running;
    }

    public void RestartSimple(Type logType, ILogger logger)
    {
        LogRestartBegin(logType, logger);
        Stop();
        Start();
        LogRestartComplete(logType, logger);
    }

    public void SetFaultLogAndNotify(Exception ex, ILogger? logger, IBackToFrontNotifyService? notify, string message)
    {
        SetFault(ex);
        logger?.Error(ex, message);
        notify?.SendError(message, exception: ex);
    }

    #region Standard Log
    public void LogStartBegin(Type logType, ILogger logger)
    {
        logger.Information("{serviceName}: Service starting", logType.Name);
    }

    public void LogStartAlreadyRunning(Type logType, ILogger logger)
    {
        logger.Information("{serviceName}: Service start cancelled, already running", logType.Name);
    }

    public void LogStartComplete(Type logType, ILogger logger)
    {
        logger.Information("{serviceName}: Service started", logType.Name);
    }

    public void LogRestartBegin(Type logType, ILogger logger)
    {
        logger.Information("{serviceName}: Service restarting", logType.Name);
    }

    public void LogRestartComplete(Type logType, ILogger logger)
    {
        logger.Information("{serviceName}: Service restarted", logType.Name);
    }

    public void LogStopBegin(Type logType, ILogger logger)
    {
        logger.Information("{serviceName}: Service stopping", logType.Name);
    }

    public void LogStopComplete(Type logType, ILogger logger)
    {
        logger.Information("{serviceName}: Service stopped", logType.Name);
    }
    #endregion
}