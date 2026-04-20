using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Osc.SendReceive;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockOscSendService(ConfigModel config) : MockStartStopServiceBase, IOscSendService
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

    public async Task<Res> SendAsync(string ip, ushort port, string address, params object?[] args)
    {
        if (IsInBanlist(ip, port)) return ResC.Fail("Address in ban list");
        ReceivedMessages.Add((ip, port, address, args));
        return ResC.Ok();
    }

    public Res SendSync(string ip, ushort port, string address, params object?[] args)
    {
        if (IsInBanlist(ip, port)) return ResC.Fail("Address in ban list");
        ReceivedMessages.Add((ip, port, address, args));
        return ResC.Ok();
    }

    public void SendSyncFireAndForget(string ip, ushort port, string address, params object?[] args)
    {
        if (IsInBanlist(ip, port)) return;
        ReceivedMessages.Add((ip, port, address, args));
    }

    public async Task<Res> SendToDefaultAsync(string address, params object?[] args)
    {
        ReceivedMessages.Add((GetDefaultIp(), GetDefaultPort(), address, args));
        return ResC.Ok();
    }

    public Res SendToDefaultSync(string address, params object?[] args)
    {
        ReceivedMessages.Add((GetDefaultIp(), GetDefaultPort(), address, args));
        return ResC.Ok();
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