using System.Net;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.SendReceive;

/// <summary>
/// Default Sender for OSC
/// </summary>
[LoadIntoDiContainer(typeof(IOscSendService), Lifetime.Singleton)]
public class OscSendService(ILogger logger, ConfigModel config, IBackToFrontNotifyService notify) 
    : StartStopServiceBase(logger.ForContext<OscSendService>()), IOscSendService
{
    private readonly Dictionary<string, OscSender> _senders = [];
    private readonly ConfigModel _config = config;
    private readonly IBackToFrontNotifyService _notify = notify;

    #region Start / Stop
    protected override bool IsProcessing() => true;
    protected override bool IsStarted() => true;
    protected override Res StartForService() => ResC.Ok();
    protected override Res StopForService() => ResC.Ok();
    protected override bool UseAlreadyStartedProtection => true;
    protected override void DisposeCleanup()
    {
        foreach(var key in _senders.Keys)
        {
            _senders[key].Dispose();
        }
        _senders.Clear();
    }
    #endregion

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
        if (!sender.IsOk) return;
        SendSyncFireAndForget(sender.Value, ip, port, address, args);
    }

    public Res SendToDefaultSync(string address, params object?[] args)
    {
        return SendSync(GetDefaultIp(), GetDefaultPort(), address, args);
    }

    public Res SendSync(string ip, ushort port, string address, params object?[] args)
    {
        var sender = GetOrCreateSender(ip, port);
        if (!sender.IsOk) return ResC.Fail(sender.Msg);
        return SendSync(sender.Value, ip, port, address, args);
    }

    public async Task<Res> SendToDefaultAsync(string address, params object?[] args)
    {
        return await SendAsync(GetDefaultIp(), GetDefaultPort(), address, args);
    }

    public async Task<Res> SendAsync(string ip, ushort port, string address, params object?[] args)
    {
        var sender = GetOrCreateSender(ip, port);
        if (!sender.IsOk) return ResC.Fail(sender.Msg);
        return await SendAsync(sender.Value, ip, port, address, args);
    }
    #endregion

    #region Sending Internals
    private void SendSyncFireAndForget(OscSender sender, string ipForLog, ushort portForLog, string address, params object?[] args)
    {
        Task.Run(() => SendAsync(sender, ipForLog, portForLog, address, args)).ConfigureAwait(false);    
    }

    private Res SendSync(OscSender sender, string ipForLog, ushort portForLog, string address, params object?[] args)
    {
        Res func() => SendAsync(sender, ipForLog, portForLog, address, args).GetAwaiter().GetResult();
        return ResC.Wrap(func, "SendSync failed", _logger);
    }

    private async Task<Res> SendAsync(OscSender sender, string ipForLog, ushort portForLog, string address, params object?[] args)
    {
        var packet = new OscMessage(address, args);
        try
        {
            _logger.Verbose("Sending packet to {targetIp}->{targetPort}->\"{address}\" with parameters {params}",
            ipForLog, portForLog, address, args);
            await sender.SendAsync(packet);
            _logger.Verbose("Sent packet to {targetIp}->{targetPort}->\"{address}\" with parameters {params}",
            ipForLog, portForLog, address, args);
            return ResC.Ok();
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send packet to {ipForLog}->{portForLog}->\"{address}\" with parameters {args}";
            var res = ResC.FailLog(msg, _logger, ex, ResMsgLvl.Warning);
            _notify.SendWarning("OSC Send Failed", msg, ex);
            return res;
        }
    }
    #endregion

    #region Utils
    private Res<OscSender> GetOrCreateSender(string ip, ushort port)
    {
        var idString = GetIdString(ip, port);
        if (_senders.TryGetValue(idString, out var sender))
            return ResC.TOk(sender);

        var endpoint = ParseIpEndpoint(ip, port);
        if (!endpoint.IsOk) return ResC.TFail<OscSender>(endpoint.Msg);

        sender = new(endpoint.Value);
        _senders[idString] = sender;

        return ResC.TOk(sender);
    }

    private static string GetIdString(string ipString, ushort port)
    {
        return $"[{ipString}]:{port}";
    }

    private Res<IPEndPoint> ParseIpEndpoint(string ipString, ushort port)
    {
        if (!IPAddress.TryParse(ipString, out var ipAddress))
        {
            var message = $"Failed to convert IP string \"{ipString}\" to an IP address and is unable to send";
            _notify.SendWarning("OSC Send Error", message);
            return ResC.TFailLog<IPEndPoint>(message, _logger);
        }
        return ResC.TOk(new IPEndPoint(ipAddress, port));
    }
    #endregion
}