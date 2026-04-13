using System.Diagnostics;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
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
    public Res Start()
    {
        return SafeExecute("start", StartForService, () =>
        {
            if (UseAlreadyStartedProtection && IsStarted())
            {
                _logger.Debug("Service start cancelled, already started");
                return ResC.Ok();
            }
            return null;
        });
    }
    protected abstract bool UseAlreadyStartedProtection { get; }
    protected abstract Res StartForService();
    #endregion

    #region Stopping
    public Res Stop()
    {
        return SafeExecute("stop", StopForService);
    }
    protected abstract Res StopForService();
    #endregion

    #region Restart
    public Res Restart()
    {
        return SafeExecute("restart", RestartForService);
    }
    protected virtual Res RestartForService() //todo: [REFACTOR] Check if even needed
    {
        var resStop = Stop();
        if (!resStop.IsOk) return resStop;

        return Start();
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

    #region Utils
    private Res SafeExecute(string verb, Func<Res> wrappedFunc, Func<Res?>? extraFunc = null)
    {
        _logger.Debug("Service {verb} begin", verb);

        var extraRes = extraFunc?.Invoke();
        if (extraRes is not null)
        {
            return extraRes;
        }

        var sw = Stopwatch.StartNew();
        var res = ResC.Wrap(wrappedFunc, $"Service {verb} failed", _logger);
        sw.Stop();

        if (!res.IsOk)
        {
            _logger.Warning("Service {verb} encountered error after {elapsed}ms => {result}", 
                verb, sw.ElapsedMilliseconds, res);
        }
        else
        {
            _logger.Debug("Service {verb} completed in {elapsed}ms", verb, sw.ElapsedMilliseconds);
        }

        return res;
    }
    #endregion
}