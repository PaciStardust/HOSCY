using System.Net;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.SendReceive;

/// <summary>
/// Default Sender for OSC
/// </summary>
[LoadIntoDiContainer(typeof(IOscSendService), Lifetime.Singleton)]
public class OscSendService(ILogger logger, ConfigModel config, IBackToFrontNotifyService notify) : IOscSendService, IDisposable
{
    private readonly Dictionary<string, OscSender> _senders = [];
    private readonly ILogger _logger = logger.ForContext<OscSendService>();
    private readonly ConfigModel _config = config;
    private readonly IBackToFrontNotifyService _notify = notify;

    #region Defaults
    public string GetDefaultIp()
        => _config.Osc_Routing_TargetIp;
    public ushort GetDefaultPort()
        => _config.Osc_Routing_TargetPort;
    #endregion

    #region Sending Public
    public void SendToDefaultSyncFireAndForget(string address, params object?[] args)
    {
        SendSyncFireAndForget(GetDefaultIp(), GetDefaultPort(), address, args);
    }

    public void SendSyncFireAndForget(string ip, ushort port, string address, params object?[] args)
    {
        var sender = GetOrCreateSender(ip, port);
        if (sender is null) return;
        SendSyncFireAndForget(sender, ip, port, address, args);
    }

    public bool SendToDefaultSync(string address, params object?[] args)
    {
        return SendSync(GetDefaultIp(), GetDefaultPort(), address, args);
    }

    public bool SendSync(string ip, ushort port, string address, params object?[] args)
    {
        var sender = GetOrCreateSender(ip, port);
        if (sender is null) return false;
        return SendSync(sender, ip, port, address, args);
    }

    public async Task<bool> SendToDefaultAsync(string address, params object?[] args)
    {
        return await SendAsync(GetDefaultIp(), GetDefaultPort(), address, args);
    }

    public async Task<bool> SendAsync(string ip, ushort port, string address, params object?[] args)
    {
        var sender = GetOrCreateSender(ip, port);
        if (sender is null) return false;
        return await SendAsync(sender, ip, port, address, args);
    }
    #endregion

    #region Sending Internals
    private void SendSyncFireAndForget(OscSender sender, string ipForLog, ushort portForLog, string address, params object?[] args)
    {
        Task.Run(() => SendAsync(sender, ipForLog, portForLog, address, args)).ConfigureAwait(false);    
    }

    private bool SendSync(OscSender sender, string ipForLog, ushort portForLog, string address, params object?[] args)
    {
        return SendAsync(sender, ipForLog, portForLog, address, args).GetAwaiter().GetResult();
    }

    private async Task<bool> SendAsync(OscSender sender, string ipForLog, ushort portForLog, string address, params object?[] args)
    {
        var packet = new OscMessage(address, args);
        try
        {
            _logger.Verbose("Sending packet to {targetIp}->{targetPort}->\"{address}\" with parameters {params}",
            ipForLog, portForLog, address, args);
            await sender.SendAsync(packet);
            _logger.Verbose("Sent packet to {targetIp}->{targetPort}->\"{address}\" with parameters {params}",
            ipForLog, portForLog, address, args);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to send packet to {targetIp}->{targetPort}->\"{address}\" with parameters {params}",
            ipForLog, portForLog, address, args);
            _notify.SendWarning("OSC Send Failed", $"Failed to send packet to {ipForLog}->{portForLog}->\"{address}\" with parameters {args}", ex);
            return false;
        }
    }
    #endregion

    #region Utils
    private OscSender? GetOrCreateSender(string ip, ushort port)
    {
        var idString = GetIdString(ip, port);
        if (!_senders.TryGetValue(idString, out var sender))
        {
            var endpoint = ParseIpEndpoint(ip, port);
            if (endpoint is null) return null;
            sender = new(endpoint);
            _senders[idString] = sender;
        }
        return sender;
    }

    private static string GetIdString(string ipString, ushort port)
    {
        return $"[{ipString}]:{port}";
    }

    private IPEndPoint? ParseIpEndpoint(string ipString, ushort port)
    {
        if (!IPAddress.TryParse(ipString, out var ipAddress))
        {
            _notify.SendWarning("OSC Send Error", $"Failed to convert IP string \"{ipString}\" to an IP address and is unable to send");
            _logger.Error("Failed to parse IP {ipString} for OSC sending", ipString);
            return null;
        }
        return new(ipAddress, port);
    }

    public void Dispose()
    {
        foreach(var key in _senders.Keys)
        {
            _senders[key].Dispose();
        }
        _senders.Clear();
    }
    #endregion
}