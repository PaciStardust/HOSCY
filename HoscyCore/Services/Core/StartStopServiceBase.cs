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
    private ResMsg? _internalErrorMessage = null;
    #endregion

    #region Status
    public ServiceStatus GetCurrentStatus()
    {
        if (!IsStarted()) return ServiceStatus.Stopped;
        if (GetErrorMessageIfExists() is not null) return ServiceStatus.Faulted;
        if (IsProcessing()) return ServiceStatus.Processing;
        return ServiceStatus.Started;
    }

    protected abstract bool IsStarted();
    protected abstract bool IsProcessing();
    #endregion

    #region Startup
    public Res Start()
    {
        var res = SafeExecute("start", StartForService, () =>
        {
            if (UseAlreadyStartedProtection && IsStarted())
            {
                _logger.Debug("Service start cancelled, already started");
                return ResC.Ok();
            }
            return null;
        });
        if (res.IsOk) return res;

        _logger.Debug("Start failed performing dispose...");
        var resDispose = ResC.WrapR(DisposeCleanup, "Failed to dispose", _logger);
        return ResC.FailM(res.Msg?.WithContext("Start"), resDispose.Msg?.WithContext("Dispose"));
    }
    protected abstract bool UseAlreadyStartedProtection { get; }
    protected abstract Res StartForService();
    #endregion

    #region Stopping
    public Res Stop()
    {
        var res = SafeExecute("stop", StopForService);
        _logger.Debug("Performing dispose...");
        var resDispose = ResC.WrapR(DisposeCleanup, "Failed to dispose", _logger);
        return res.IsOk && resDispose.IsOk ? ResC.Ok() : ResC.FailM(res.Msg?.WithContext("Stop"), resDispose.Msg?.WithContext("Dispose"));
    }
    protected abstract Res StopForService();
    protected abstract void DisposeCleanup();
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
    public virtual ResMsg? GetErrorMessageIfExists()
        => _internalErrorMessage;

    protected virtual void SetFault(ResMsg? msg)
    {
        _internalErrorMessage = msg;
    }
    protected void SetFaultLogNotify(ResMsg msg, string title, IBackToFrontNotifyService? notify, ILogger? logger)
    {
        SetFault(msg);
        logger?.Error($"{title}: {msg}");
        notify?.SendResult(title, msg);
    }

    public void ClearFault()
        => SetFault(null);
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