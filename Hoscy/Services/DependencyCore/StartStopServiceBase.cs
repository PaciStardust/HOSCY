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
    public abstract bool TryRestart();
    public abstract bool IsRunning();

    public Exception? GetFaultIfExists()
        => _internalException;

    protected void SetFault(Exception ex)
    {
        _internalException = ex;
    }

    public StatStopServiceStatus GetStatus()
    {
        if (!IsRunning()) return StatStopServiceStatus.Stopped;
        if (GetFaultIfExists() is not null) return StatStopServiceStatus.Faulted;
        return StatStopServiceStatus.Running;
    }

    public bool TryRestartSimple(string serviceName, ILogger logger, IBackToFrontNotifyService? notify)
    {
        logger.Information("Restarting Service {serviceName}", serviceName);
        try
        {
            Stop();
            Start();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to restart service {serviceName}", serviceName);
            notify?.SendError($"{serviceName} restart failed", exception: ex);
            return false;
        }
        logger.Information("Restarted Service {serviceName}", serviceName);
        return true;
    }
}