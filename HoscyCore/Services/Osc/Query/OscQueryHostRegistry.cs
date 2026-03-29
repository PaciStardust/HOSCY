using HoscyCore.Services.Dependency;
using Serilog;
using Vrc = VRC.OSCQuery;

namespace HoscyCore.Services.Osc.Query;

[LoadIntoDiContainer(typeof(OscQueryHostRegistry))]
public class OscQueryHostRegistry(ILogger logger)
{
    private readonly ILogger _logger = logger.ForContext<OscQueryHostRegistry>();
    private readonly Dictionary<string, (Vrc.HostInfo Host, DateTimeOffset LastHeard)> _hosts = [];
    private (string Ip, int Port)? _self;
    
    /// <summary>
    /// Adds HostInfo to list
    /// </summary>
    public void AddHostInfoFromServiceProfile(Vrc.OSCQueryServiceProfile profile)
    {
        var lowerName = profile.name.ToLower();
        if (!_hosts.ContainsKey(lowerName))
        {
            _logger.Debug("Received ServiceProfile \"{profileName}\"", profile.name);
        }

        var hostInfo = Vrc.Extensions.GetHostInfo(profile.address, profile.port).GetAwaiter().GetResult();
        if (hostInfo == null)
        {
            _logger.Warning("Failed to grab HostInfo for ServiceProfile \"{profileName}\"", profile.name);
            throw new ArgumentException("Failed to grab HostInfo for ServiceProfile " + profile.name);
        }

        // We do not log refreshes
        if (!_hosts.ContainsKey(lowerName))
        {
            _logger.Debug("Adding HostInfo {hostInfoName} (IP={hostIp} Port={hostPort}) from ServiceProfile \"{profileName}\" to hosts list",
                lowerName, hostInfo.oscIP, hostInfo.oscPort, profile.name);
        }
        _hosts[lowerName] = (hostInfo, DateTimeOffset.UtcNow);
    }

    public (string Ip, int Port)? GetServiceAddressByName(string name)
    {
        var lowerName = name.ToLower();

        if (lowerName == "self")
            return _self;

        (string, int)? returnValue = _hosts.TryGetValue(lowerName, out var hostData)
            ? (hostData.Host.oscIP, hostData.Host.oscPort)
            : null;

        if (returnValue is not null)
            return returnValue;

        var returnKeys = _hosts
            .Select(x => x.Key)
            .Where(x => x.StartsWith(lowerName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        if (returnKeys.Length == 0)
            return null;

        var keyValue = _hosts[returnKeys[0]].Host;
        return (keyValue.oscIP, keyValue.oscPort);
    }

    public void Clear()
    {
        _logger.Debug("Clearing host registry");
        _hosts.Clear();
        _self = null;
        _logger.Debug("Cleared host registry");
    }

    public void CleanOldEntries()
    {
        var limit = DateTimeOffset.UtcNow.AddMinutes(-3);
        var missing = _hosts.Where(kvp => kvp.Value.LastHeard < limit)
            .Select(kvp => kvp.Key).ToArray();
        
        if (missing.Length == 0) return;

        foreach(var key in missing)
        {
            logger.Debug("Removing host \"{host}\" because of expired lifetime", key);
            _hosts.Remove(key);
        }
    }

    public void SetSelf(string ip, int port)
    {
        _self = (ip, port);
    }
}