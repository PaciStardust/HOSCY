using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using LucHeart.CoreOSC;
using Serilog;

namespace Hoscy.Services.Osc;

/// <summary>
/// Default Listener for OSC
/// </summary>
[LoadIntoDiContainer(typeof(IOscListenService), Lifetime.Singleton)]
public class OscListenService(ConfigModel config, IOscSendService sender, ILogger logger, IBackToFrontNotifyService notify) : StartStopServiceBase, IOscListenService
{
    private readonly ConfigModel _config = config;
    private readonly IOscSendService _sender = sender;
    private readonly ILogger _logger = logger.ForContext<OscListenService>();
    private readonly IBackToFrontNotifyService _notify = notify;

    private OscListener? _listener = null;
    private CancellationTokenSource? _cts = null;
    private Task? _workerTask = null;

    #region Start Stop
    public override bool IsRunning()
    {
        return _workerTask is not null || _cts is not null || _listener is not null;
    }

    public int? GetPort()
    {
        return IsRunning() ? _config.Osc_Routing_ListenPort : null;
    }

    protected override void StartInternal()
    {
        _logger.Information("Starting up Service on localhost:{port}", _config.Osc_Routing_ListenPort);
        if (IsRunning())
        {
            _logger.Information("Skipped starting Service, still running");
            return;
        }
        _listener = new(new(IPAddress.Loopback, _config.Osc_Routing_ListenPort))
        {
            EnableTransparentBundleToMessageConversion = true
        };
        _cts = new CancellationTokenSource();
        _logger.Debug("Starting worker loop");
        _workerTask = Task.Run(ListenLoop);
        _logger.Information("Service has been started");
    }

    public override void Stop()
    {
        _logger.Information("Stopping Service...");
        _cts?.Cancel();
        try
        {
            _workerTask?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException ex)
        {
            _logger.Debug(ex, "Caught expected exception during shutdown");
        }
        finally
        {
            _logger.Debug("Cleanup of internals...");
            _cts?.Dispose();
            _cts = null;
            _workerTask = null;
            _listener?.Dispose();
            _listener = null;
        }
        _logger.Information("Service stopped");
    }

    public override bool TryRestart()
        => TryRestartSimple(GetType().Name, _logger, _notify);
    #endregion

    #region Listening
    public async Task ListenLoop()
    {
        _logger.Debug("Started Listen Loop");
        try
        {
            while (_cts is not null && !_cts.IsCancellationRequested && _listener is not null)
            {
                await DoListen();
            }
        }
        catch (OperationCanceledException ex) when (_cts?.IsCancellationRequested ?? false)
        {
            _logger.Debug(ex, "Listen loop stopped via CancelException");
        }
        catch (Exception ex)
        {
            SetFault(ex);
            _logger.Error(ex, "Listen Loop encountered an unexpected error");
            _notify.SendError("Listen Loop encountered an exception", exception: ex);
            while (_cts is not null && !_cts.IsCancellationRequested)
            {
                await Task.Delay(100);
            }
        }
        _logger.Debug("Stopped Listen Loop");
    }

    private async Task DoListen()
    {
        var res = await _listener!.ReceiveMessageAsync(_cts!.Token);
        //todo: routing and handling
    }
    #endregion
}