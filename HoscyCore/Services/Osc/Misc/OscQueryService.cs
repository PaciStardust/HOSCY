using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Osc.SendReceive;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Vrc = VRC.OSCQuery;
using Timer = System.Timers.Timer;

namespace HoscyCore.Services.Osc.Misc;

[LoadIntoDiContainer(typeof(IOscQueryService), Lifetime.Singleton)]
public class OscQueryService(Serilog.ILogger logger, IBackToFrontNotifyService notify, IOscListenService listener, OscQueryHostRegistry hostRegistry) : StartStopServiceBase, IOscQueryService
{
    private readonly Serilog.ILogger _logger = logger.ForContext<OscQueryService>();
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly IOscListenService _listener = listener;
    private readonly OscQueryHostRegistry _hostRegistry = hostRegistry;

    private Vrc.OSCQueryService? _oscQuery = null;
    private Timer? _serviceRefreshTimer = null;

    #region Start/Stop
    protected override void StartInternal()
    {
        LogStartBegin(GetType(), _logger);
        if (IsStarted())
        {
            LogStartAlreadyStarted(GetType(), _logger);
            return;
        }

        var udpPort = _listener.GetPort();
        if (!udpPort.HasValue)
        {
            _logger.Error("Could not retrieve UDP Port from OscListenService");
            throw new StartStopServiceException("Could not retrieve UDP Port from OscListenService");
        }

        var msftLogger = new SerilogLoggerFactory(_logger)
            .CreateLogger<Vrc.OSCQueryService>();

        var id = Guid.NewGuid().ToString()[..8];
        var tcpPort = Vrc.Extensions.GetAvailableTcpPort();
        var oscQuery = new Vrc.OSCQueryServiceBuilder()
            .WithServiceName($"HOSCY-{id}")
            .WithTcpPort(tcpPort)
            .WithUdpPort(udpPort.Value)
            .WithLogger(msftLogger)
            .WithDefaults()
            .Build();

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
        LogStartComplete(GetType(), _logger);
    }

    public override void Stop()
    {
        LogStopBegin(GetType(), _logger);
        _serviceRefreshTimer?.Stop();
        _serviceRefreshTimer?.Dispose();
        _serviceRefreshTimer = null;
        _oscQuery?.Dispose();
        _oscQuery = null;
        _hostRegistry.Clear();
        LogStopComplete(GetType(), _logger);
    }

    protected override bool IsStarted()
        => _oscQuery is not null || _serviceRefreshTimer is not null;
    protected override bool IsProcessing()
        => IsStarted();

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }
    #endregion

    #region Functionality

    /// <summary>
    /// Safely adds HostInfo to list
    /// </summary>
    private void TryAddHostInfoFromServiceProfile(Vrc.OSCQueryServiceProfile profile)
    {
        try
        {
            _hostRegistry.AddHostInfoFromServiceProfile(profile);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to add HostInfo");
            _notify.SendWarning("Failed to add Hostinfo", exception: ex);
        }
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