using System.Net;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
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
    : StartStopServiceBase, IOscListenService
{
    private readonly ConfigModel _config = config;
    private readonly ILogger _logger = logger.ForContext<OscListenService>();
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
        => IsStarted();

    public int? GetPort()
    {
        return IsStarted() ? _config.Osc_Routing_ListenPort : null;
    }

    protected override void StartInternal()
    {
        _logger.Debug("Starting up listener on localhost:{port}", _config.Osc_Routing_ListenPort);
        if (IsStarted())
        {
            _logger.Debug("Skipped starting listener, still running");
            return;
        }
        _listener = new(new(IPAddress.Loopback, _config.Osc_Routing_ListenPort))
        {
            EnableTransparentBundleToMessageConversion = true
        };
        _cts = new CancellationTokenSource();
        _logger.Debug("Starting listen loop");
        _workerTask = Task.Run(ListenLoop);
        _logger.Debug("Listener and listen loop has been started");
    }

    public override void Stop()
    {
        _logger.Debug("Stopping listen loop...");
        _cts?.Cancel();
        var ex = LaunchUtils.SafelyWaitForTaskWithTimeoutAndLogException(_workerTask, 1000, new StartStopServiceException("Unable to stop listen loop"));
        if (ex is not null)
        {
            _logger.Error(ex, "Caught exception while stopping listen loop");
        }
        _logger.Debug("Cleanup of internals...");
        _cts?.Dispose();
        _cts = null;
        _workerTask = null;
        _listener?.Dispose();
        _listener = null;
        _logger.Debug("Listen loop stopped");
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
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