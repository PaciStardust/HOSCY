using System.Net;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Osc.MessageHandling;
using HoscyCore.Services.Osc.Relay;
using HoscyCore.Utility;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.SendReceive;

/// <summary>
/// Default Listener for OSC
/// </summary>
[LoadIntoDiContainer(typeof(IOscListenService), Lifetime.Singleton)]
public class OscListenService(ConfigModel config, ILogger logger, IBackToFrontNotifyService notify, IOscMessageHandlingService messageHandler, IOscRelayService relay)
    : StartStopServiceBase(logger.ForContext<OscListenService>()), IOscListenService
{
    private readonly ConfigModel _config = config;
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly IOscMessageHandlingService _messageHandler = messageHandler;
    private readonly IOscRelayService _relay = relay;

    private OscListener? _listener = null;
    private CancellationTokenSource? _cts = null;
    private Task? _workerTask = null;

    #region Start Stop
    protected override bool IsStarted()
        => _workerTask is not null || _cts is not null || _listener is not null;
    protected override bool IsProcessing()
        => _workerTask is not null && _cts is not null && _listener is not null;

    public Res<int> GetPort()
    {
        return IsStarted() 
            ? ResC.TOk(_config.Osc_Routing_ListenPort) 
            : ResC.TFail<int>(ResMsg.Err("Listen port not available, service not started"));
    }

    protected override Res StartForService()
    {
        try
        {
            _logger.Debug("Starting up listener on localhost:{port}", _config.Osc_Routing_ListenPort);
            _listener = new(new(IPAddress.Loopback, _config.Osc_Routing_ListenPort))
            {
                EnableTransparentBundleToMessageConversion = true
            };
            _cts = new CancellationTokenSource();

            _workerTask = Task.Run(ListenLoop);

            return ResC.Ok();
        }
        catch (Exception ex)
        {
            return ResC.FailLog("Failed starting OSC Listener", _logger, ex);
        }
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForService()
    {
        _logger.Debug("Stopping listen loop...");
        _cts?.Cancel();

        return LaunchUtils.SafelyWaitForTaskWithTimeoutAndReturnException(_workerTask, 1000,
            new StartStopServiceException("Unable to stop listen loop"), _logger);

    }
    protected override void DisposeCleanup()
    {
        _cts?.Dispose();
        _cts = null;
        _workerTask = null;
        _listener?.Dispose();
        _listener = null;
    }
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
        catch (OperationCanceledException ex)
        {
            _logger.Debug(ex, "Listen Loop was cancelled");
            return;
        }
        catch (Exception ex)
        {
            SetFaultLogAndNotify(ex, _logger, _notify, "Listen Loop encountered an unexpected error");
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
        if (res is null)
        {
            _logger.Debug("Received an empty packet, skipping");
            return;
        }

        var argsInfo = new List<string>();
        foreach (var arg in res.Arguments)
        {
            argsInfo.Add(arg is null
                ? "[NULL]"
                : $"{arg.GetType().Name}({arg})" ?? "???");
        }
        _logger.Verbose("Packet has been received on port {thisPort} with address \"{messageAddress}\" => {argInfo}",
            GetPort(), res.Address, argsInfo);

        var handled = _messageHandler.HandleMessage(res);
        if (handled && _config.Osc_Relay_IgnoreIfHandled) return;
        _relay.HandleRelay(res);
    }
    #endregion
}