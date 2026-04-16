using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;
using Vrc = VRC.OSCQuery;

namespace HoscyCore.Services.Osc.Query;

[LoadIntoDiContainer(typeof(OscQueryHostRegistry))]
public class OscQueryHostRegistry(ILogger logger)
{
    private readonly ILogger _logger = logger.ForContext<OscQueryHostRegistry>();
    private readonly Dictionary<string, (Vrc.HostInfo Host, DateTimeOffset LastHeard)> _hosts = [];
    private HashSet<string> _nameBlacklist = [];
    private (string Ip, int Port)? _self;
    
    /// <summary>
    /// Adds HostInfo to list
    /// </summary>
    public Res AddHostInfoFromServiceProfile(Vrc.OSCQueryServiceProfile profile)
    {
        var lowerName = profile.name.ToLower();
        if (_nameBlacklist.Contains(lowerName)) return ResC.Ok();

        if (!_hosts.ContainsKey(lowerName))
        {
            _logger.Debug("Received ServiceProfile \"{profileName}\"", profile.name);
        }

        var hostInfo = GetVrcHostInfo(profile);
        if (!hostInfo.IsOk)
        {
            _nameBlacklist.Add(lowerName);
            return ResC.Fail(hostInfo.Msg);
        }

        // We do not log refreshes
        if (!_hosts.ContainsKey(lowerName))
        {
            _logger.Debug("Adding HostInfo {hostInfoName} (IP={hostIp} Port={hostPort}) from ServiceProfile \"{profileName}\" to hosts list",
                lowerName, hostInfo.Value.oscIP, hostInfo.Value.oscPort, profile.name);
        }

        _hosts[lowerName] = (hostInfo.Value, DateTimeOffset.UtcNow);
        return ResC.Ok();
    }

    private Res<Vrc.HostInfo> GetVrcHostInfo(Vrc.OSCQueryServiceProfile profile)
    {
        return ResC.TWrap(() =>
        {
            var host = Vrc.Extensions.GetHostInfo(profile.address, profile.port).GetAwaiter().GetResult();
            return ResC.TOk(host);
        }, $"Failed to grab HostInfo for ServiceProfile \"{profile.name}\"", _logger);
    }

    public Res<(string Ip, int Port)> GetServiceAddressByName(string name)
    {
        var lowerName = name.ToLower();

        if (lowerName == "self")
        {
            return _self is null 
                ? ResC.TFailLog<(string, int)>("Unable to get OSC host info for \"self\", value is not set", _logger)
                : ResC.TOk(_self.Value);
        }
        
            
        (string, int)? returnValue = _hosts.TryGetValue(lowerName, out var hostData)
            ? (hostData.Host.oscIP, hostData.Host.oscPort)
            : null;

        if (returnValue is not null)
            return ResC.TOk(returnValue.Value);

        var returnKeys = _hosts
            .Select(x => x.Key)
            .Where(x => x.StartsWith(lowerName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        if (returnKeys.Length == 0)
            return ResC.TFailLog<(string, int)>($"Unable to get OSC host info for \"{lowerName}\", value could not be found", _logger);

        var keyValue = _hosts[returnKeys[0]].Host;
        return ResC.TOk((keyValue.oscIP, keyValue.oscPort));
    }

    public void Clear()
    {
        _logger.Debug("Clearing host registry");
        _hosts.Clear();
        _nameBlacklist.Clear();
        _self = null;
        _logger.Debug("Cleared host registry");
    }

    public void CleanOldEntries()
    {
        var limit = DateTimeOffset.UtcNow.AddMinutes(-3);
        var missing = _hosts.Where(kvp => kvp.Value.LastHeard < limit) //todo: Collection was modified; enumeration operation may not execute.
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