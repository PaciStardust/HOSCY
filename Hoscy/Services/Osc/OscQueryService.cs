using System;
using System.Collections.Generic;
using System.Timers;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using Hoscy.Services.Osc.SendReceive;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Vrc = VRC.OSCQuery;

namespace Hoscy.Services.Osc;

[LoadIntoDiContainer(typeof(IOscQueryService), Lifetime.Singleton)]
public class OscQueryService(Serilog.ILogger logger, IBackToFrontNotifyService notify, IOscListenService listener) : StartStopServiceBase, IOscQueryService
{
    private readonly Serilog.ILogger _logger = logger.ForContext<OscQueryService>();
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly IOscListenService _listener = listener;

    private Vrc.OSCQueryService? _oscQuery = null;
    private readonly Dictionary<string, Vrc.HostInfo> _hosts = [];
    private Timer? _serviceRefreshTimer = null;

    #region Start/Stop
    protected override void StartInternal()
    {
        _logger.Information("Starting up OscQuery");
        if (IsRunning())
        {
            _logger.Information("Skipped starting OscQuery, still running");
            return;
        }

        var udpPort = _listener.GetPort();
        if (!udpPort.HasValue)
        {
            throw new StartStopServiceException("Could not retrieve UDP Port from OscListenService");
        }

        var msftLogger = new SerilogLoggerFactory(logger)
            .CreateLogger<Vrc.OSCQueryService>();

        var oscQuery = new Vrc.OSCQueryServiceBuilder()
            .WithServiceName("HoscyOscQuery")
            .WithTcpPort(Vrc.Extensions.GetAvailableTcpPort())
            .WithUdpPort(udpPort.Value)
            .WithLogger(msftLogger)
            .WithDiscovery(new Vrc.MeaModDiscovery(msftLogger))
            .Build();

        oscQuery.AddEndpoint("/*", "[,]", Vrc.Attributes.AccessValues.ReadWrite, null,
            "Any -> HOSCY sends and receives anything for routing and custom commands");

        _logger.Information("Retrieving current OscQueryServices");

        _hosts.Clear();
        foreach (var profile in oscQuery.GetOSCQueryServices())
        {
            AddHostInfoFromServiceProfile(profile, _hosts);
        }
        oscQuery.OnOscQueryServiceAdded += (profile) => TryAddHostInfoFromServiceProfile(profile, _hosts);
        _oscQuery = oscQuery;

        _logger.Debug("Starting service refresh timer");
        var timer = CreateRefreshTimer(oscQuery, 5000);
        timer.Start();
        _serviceRefreshTimer = timer;
        _logger.Information("Service started");
    }

    public override void Stop()
    {
        _logger.Information("Stopping Service...");
        _serviceRefreshTimer?.Stop();
        _serviceRefreshTimer?.Dispose();
        _serviceRefreshTimer = null;
        _oscQuery?.Dispose();
        _oscQuery = null;
        _hosts.Clear();
        _logger.Information("Service stopped");
    }

    public override bool IsRunning()
    {
        return _oscQuery is not null || _serviceRefreshTimer is not null;
    }

    public override bool TryRestart()
        => TryRestartSimple(GetType().Name, _logger, _notify);
    #endregion

    #region Functionality
    /// <summary>
    /// Adds HostInfo to list
    /// </summary>
    private void AddHostInfoFromServiceProfile(Vrc.OSCQueryServiceProfile profile, Dictionary<string, Vrc.HostInfo> hosts)
    {
        _logger.Debug("Received ServiceProfile {profileName}", profile.name);
        var hostInfo = Vrc.Extensions.GetHostInfo(profile.address, profile.port).GetAwaiter().GetResult();
        if (hostInfo == null)
        {
            _logger.Warning("Failed to grab HostInfo for ServiceProfile {profileName}", profile.name);
            throw new ArgumentException("Failed to grab HostInfo for ServiceProfile " + profile.name);
        }

        var lowerName = hostInfo.name.ToLower();
        if (!hosts.ContainsKey(lowerName))
        {
            hosts[lowerName] = hostInfo;
            _logger.Debug("Adding HostInfo {hostInfoName} (IP={hostIp} Port={hostPort}) from ServiceProfile {profileName} to hosts list",
                lowerName, hostInfo.oscIP, hostInfo.oscPort, profile.name);
        }
    }

    /// <summary>
    /// Safely adds HostInfo to list
    /// </summary>
    private void TryAddHostInfoFromServiceProfile(Vrc.OSCQueryServiceProfile profile, Dictionary<string, Vrc.HostInfo> hosts)
    {
        try
        {
            AddHostInfoFromServiceProfile(profile, hosts);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to add HostInfo");
            _notify.SendWarning("Failed to add Hostinfo", exception: ex);
        }
    }

    public (string, int)? GetServiceAddressByName(string name)
    {
        var lowerName = name.ToLower();
        return _hosts.TryGetValue(lowerName, out var hostData)
            ? (hostData.oscIP, hostData.oscPort)
            : null;
    }
    #endregion

    #region Utils
    /// <summary>
    /// Creates a timer for refreshing services 
    /// </summary>
    private static Timer CreateRefreshTimer(Vrc.OSCQueryService service, int intervalMs)
    {
        var timer = new Timer(intervalMs)
        {
            AutoReset = true
        };
        timer.Elapsed += (_, _) => service.RefreshServices();
        return timer;
    }
    #endregion
}