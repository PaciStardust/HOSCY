using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Osc.SendReceive;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Vrc = VRC.OSCQuery;
using Timer = System.Timers.Timer;
using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Osc.Query;

[LoadIntoDiContainer(typeof(IOscQueryService), Lifetime.Singleton)]
public class OscQueryService(Serilog.ILogger logger, IBackToFrontNotifyService notify, IOscListenService listener, OscQueryHostRegistry hostRegistry)
    : StartStopServiceBase(logger.ForContext<OscQueryService>()), IOscQueryService
{
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly IOscListenService _listener = listener;
    private readonly OscQueryHostRegistry _hostRegistry = hostRegistry;

    private Vrc.OSCQueryService? _oscQuery = null;
    private Timer? _serviceRefreshTimer = null;

    #region Start/Stop
    protected override Res StartForService()
    {
        var udpPort = _listener.GetPort();
        if (!udpPort.IsOk) return ResC.Fail(udpPort.Msg);

        var id = Guid.NewGuid().ToString()[..8];
        var serviceResult = CreateQueryService(id, udpPort.Value);
        if (!serviceResult.IsOk) return ResC.Fail(serviceResult.Msg);

        var (tcpPort, oscQuery) = serviceResult.Value;
        _logger.Debug("Runnung OscQuery with UDP {udp}, TCP {tcp} and ID {id}", udpPort.Value, tcpPort, id);

        oscQuery.AddEndpoint("/*", "[,]", Vrc.Attributes.AccessValues.ReadWrite, null,
            "Any -> HOSCY sends and receives anything for routing and custom commands");

        _hostRegistry.Clear();
        oscQuery.OnOscQueryServiceAdded += (profile) => TryAddHostInfoFromServiceProfile(profile);
        _oscQuery = oscQuery;

        _hostRegistry.SetSelf(_oscQuery.HostInfo.oscIP, _oscQuery.HostInfo.oscPort);

        _logger.Debug("Starting service refresh timer");
        var timer = CreateRefreshTimer(_oscQuery, _hostRegistry, 5000);
        timer.Start();
        _serviceRefreshTimer = timer;

        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    private Res<(int TcpPort, Vrc.OSCQueryService QueryService)> CreateQueryService(string id, int udpPort) 
    {
        var msftLogger = new SerilogLoggerFactory(_logger)
            .CreateLogger<Vrc.OSCQueryService>();

        Vrc.OSCQueryService? service = null;
        try
        {
            var tcp = Vrc.Extensions.GetAvailableTcpPort();
            service = new Vrc.OSCQueryServiceBuilder()
                .WithServiceName($"HOSCY-{id}")
                .WithTcpPort(tcp)
                .WithUdpPort(udpPort)
                .WithLogger(msftLogger)
                .WithDefaults()
                .Build();

            return ResC.TOk((tcp, service));
        } 
        catch (Exception ex)
        {
            var res = ResC.TFailLog<(int, Vrc.OSCQueryService)>("Failed creating OSCQueryService", _logger, ex);
            service?.Dispose();
            return res;
        }
    }

    protected override Res StopForService()
    {
        _logger.Debug("Stopping refresh timer");
        _serviceRefreshTimer?.Stop();
        _serviceRefreshTimer?.Dispose();
        _serviceRefreshTimer = null;

        _logger.Debug("Stopping OSCQuery");
        _oscQuery?.Dispose();
        _oscQuery = null;
        _hostRegistry.Clear();

        return ResC.Ok();
    }

    protected override bool IsStarted()
        => _oscQuery is not null || _serviceRefreshTimer is not null;
    protected override bool IsProcessing()
        => _oscQuery is not null && _serviceRefreshTimer is not null;
    #endregion

    #region Functionality

    /// <summary>
    /// Safely adds HostInfo to list
    /// </summary>
    private Res TryAddHostInfoFromServiceProfile(Vrc.OSCQueryServiceProfile profile)
    {
        var res = _hostRegistry.AddHostInfoFromServiceProfile(profile);
        res.IfFail((x) =>
        {
            _notify.SendResult("Failed to add Hostinfo", x);
        });
        return res;
    }

    /// <summary>
    /// Creates a timer for refreshing services 
    /// </summary>
    private static Timer CreateRefreshTimer(Vrc.OSCQueryService service, OscQueryHostRegistry hostRegistry, int intervalMs)
    {
        var timer = new Timer(intervalMs)
        {
            AutoReset = true
        };
        timer.Elapsed += (_, _) => TimerElapsed(service, hostRegistry);
        return timer;
    }

    private static void TimerElapsed(Vrc.OSCQueryService service, OscQueryHostRegistry hostRegistry)
    {
        service.RefreshServices();
        hostRegistry.CleanOldEntries();
    }
    #endregion
}