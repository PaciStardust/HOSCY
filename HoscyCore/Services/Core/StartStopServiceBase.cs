using System.Diagnostics;
using HoscyCore.Services.Interfacing;
using Serilog;

namespace HoscyCore.Services.Core;

/// <summary>
/// Helper class for ease of use for the StartStopServices
/// </summary>
public abstract class StartStopServiceBase(ILogger logger) : IStartStopService
{
    #region Variables
    protected readonly ILogger _logger = logger; 
    private Exception? _internalException = null;
    #endregion

    #region Status
    public ServiceStatus GetCurrentStatus()
    {
        if (!IsStarted()) return ServiceStatus.Stopped;
        if (GetFaultIfExists() is not null) return ServiceStatus.Faulted;
        if (IsProcessing()) return ServiceStatus.Processing;
        return ServiceStatus.Started;
    }

    protected abstract bool IsStarted();
    protected abstract bool IsProcessing();
    #endregion

    #region Startup
    public void Start()
    {
        _logger.Debug("Service starting");

        if (UseAlreadyStartedProtection && IsStarted())
        {
            _logger.Debug("Service start cancelled, already started");
            return;
        }

        ClearFault();
        var sw = Stopwatch.StartNew();
        StartForService();
        sw.Stop();

        _logger.Debug("Service started in {elapsed}ms", sw.ElapsedMilliseconds);
    }
    protected abstract bool UseAlreadyStartedProtection { get; }
    protected abstract void StartForService();
    #endregion

    #region Stopping
    public void Stop()
    {
        _logger.Debug("Service stopping");

        var sw = Stopwatch.StartNew();
        StopForService();
        sw.Stop();

        _logger.Debug("Service stopped in {elapsed}ms", sw.ElapsedMilliseconds);
    }
    protected abstract void StopForService();
    #endregion

    #region Restart
    public void Restart()
    {
        _logger.Debug("Service restarting");

        var sw = Stopwatch.StartNew();
        RestartInternal();
        sw.Stop();

        _logger.Debug("Service restarted in {elapsed}ms", sw.ElapsedMilliseconds);
    }
    protected virtual void RestartInternal()
    {
        Stop();
        Start();
    }
    #endregion

    #region Exceptions
    public virtual Exception? GetFaultIfExists()
        => _internalException;

    protected virtual void SetFault(Exception? ex)
    {
        _internalException = ex;
    }
    public void ClearFault()
    {
        SetFault(null);
    }

    protected void SetFaultLogAndNotify(Exception ex, ILogger? logger, IBackToFrontNotifyService? notify, string message)
    {
        SetFault(ex);
        logger?.Error(ex, message);
        notify?.SendError(message, exception: ex);
    }
    #endregion
}