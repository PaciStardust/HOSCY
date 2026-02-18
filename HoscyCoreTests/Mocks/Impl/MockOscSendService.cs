using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Osc.SendReceive;

namespace HoscyCoreTests.Mocks.Impl;

public class MockOscSendService(ConfigModel config) : IOscSendService
{
    private readonly ConfigModel _config = config;

    public readonly List<(string Ip, ushort Port, string Address, object?[] Args)> ReceivedMessages = [];
    public readonly HashSet<string> BannedIps = [];
    public readonly HashSet<ushort> BannedPorts = [];

    public void Clear()
    {
        ReceivedMessages.Clear();
        BannedIps.Clear();
        BannedPorts.Clear();
    }

    public string GetDefaultIp()
        => _config.Osc_Routing_TargetIp;
    public ushort GetDefaultPort()
        => _config.Osc_Routing_TargetPort;

    public async Task<bool> SendAsync(string ip, ushort port, string address, params object?[] args)
    {
        if (IsInBanlist(ip, port)) return false;
        ReceivedMessages.Add((ip, port, address, args));
        return true;
    }

    public bool SendSync(string ip, ushort port, string address, params object?[] args)
    {
        if (IsInBanlist(ip, port)) return false;
        ReceivedMessages.Add((ip, port, address, args));
        return true;
    }

    public void SendSyncFireAndForget(string ip, ushort port, string address, params object?[] args)
    {
        if (IsInBanlist(ip, port)) return;
        ReceivedMessages.Add((ip, port, address, args));
    }

    public async Task<bool> SendToDefaultAsync(string address, params object?[] args)
    {
        ReceivedMessages.Add((GetDefaultIp(), GetDefaultPort(), address, args));
        return true;
    }

    public bool SendToDefaultSync(string address, params object?[] args)
    {
        ReceivedMessages.Add((GetDefaultIp(), GetDefaultPort(), address, args));
        return true;
    }

    public void SendToDefaultSyncFireAndForget(string address, params object?[] args)
    {
        ReceivedMessages.Add((GetDefaultIp(), GetDefaultPort(), address, args));
    }

    private bool IsInBanlist(string ip, ushort port)
    {
        return BannedIps.Contains(ip) || BannedPorts.Contains(port);
    }
}