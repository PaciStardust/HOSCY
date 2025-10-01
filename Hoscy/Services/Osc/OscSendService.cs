using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using LucHeart.CoreOSC;
using Serilog;

namespace Hoscy.Services.Osc;

[LoadIntoDiContainer(Lifetime.Singleton)]
public class OscSendService(ILogger logger, ConfigModel config) : IOscSendService
{
    private readonly Dictionary<string, OscSender> _senders = [];
    private readonly ILogger _logger = logger.ForContext<OscSendService>();
    private readonly ConfigModel _config = config;

    #region Sending Public
    public void Send(string address, params object?[] args)
    {
        Send(_config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort, address, args);
    }

    public void Send(string ip, ushort port, string address, params object?[] args)
    {
        var sender = GetOrCreateSender(ip, port);
        if (sender is null) return;
        SendSync(sender, ip, port, address, args);
    }

    public async Task SendAsync(string address, params object?[] args)
    {
        await SendAsync(_config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort, address, args);
    }

    public async Task SendAsync(string ip, ushort port, string address, params object?[] args)
    {
        var sender = GetOrCreateSender(ip, port);
        if (sender is null) return;
        await SendAsync(sender, ip, port, address, args);
    }
    #endregion

    #region Sending Internals
    private void SendSync(OscSender sender, string ipForLog, ushort portForLog, string address, params object?[] args)
    {
        Task.Run(() => SendAsync(sender, ipForLog, portForLog, address, args)).ConfigureAwait(false);
    }

    private async Task SendAsync(OscSender sender, string ipForLog, ushort portForLog, string address, params object?[] args)
    {
        var packet = new OscMessage(address, args);
        try
        {
            _logger.Verbose("Sending packet to {targetIp}->{targetPort}->{address} with parameters {params}",
            ipForLog, portForLog, address, args);
            await sender.SendAsync(packet);
            _logger.Verbose("Sent packet to {targetIp}->{targetPort}->{address} with parameters {params}",
            ipForLog, portForLog, address, args);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to send packet to {targetIp}->{targetPort}->{address} with parameters {params}",
            ipForLog, portForLog, address, args);
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
            //todo: some kind of UI notify service here?
            _logger.Error("Failed to parse IP {ipString} for OSC sending", ipString);
            return null;
        }
        return new(ipAddress, port);
    }
    #endregion
}