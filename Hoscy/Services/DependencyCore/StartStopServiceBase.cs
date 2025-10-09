using System;
using Hoscy.Services.Interfacing;
using Serilog;

namespace Hoscy.Services.DependencyCore;

/// <summary>
/// Helper class for ease of use for the )StartStopServices
/// </summary>
public abstract class StartStopServiceBase : IStartStopService
{
    private Exception? _internalException = null;

    public void Start()
    {
        _internalException = null;
        StartInternal();
    }
    protected abstract void StartInternal();

    public abstract void Stop();
    public abstract void Restart();
    public abstract bool IsRunning();

    public Exception? GetFaultIfExists()
        => _internalException;

    protected void SetFault(Exception ex)
    {
        _internalException = ex;
    }

    public StartStopStatus GetStatus()
    {
        if (!IsRunning()) return StartStopStatus.Stopped;
        if (GetFaultIfExists() is not null) return StartStopStatus.Faulted;
        return StartStopStatus.Running;
    }

    public void RestartSimple(string serviceName, ILogger logger)
    {
        logger.Information("Restarting Service {serviceName}", serviceName);
        Stop();
        Start();
        logger.Information("Restarted Service {serviceName}", serviceName);
    }

    public void SetFaultLogAndNotify(Exception ex, ILogger? logger, IBackToFrontNotifyService? notify, string message)
    {
        SetFault(ex);
        logger?.Error(ex, message);
        notify?.SendError(message, exception: ex);
    }
}