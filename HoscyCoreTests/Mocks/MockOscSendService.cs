using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Osc.SendReceive;

namespace HoscyCoreTests.Mocks;

public class MockOscSendService(ConfigModel config) : IOscSendService
{
    private readonly ConfigModel _config = config;

    public readonly List<(string Ip, ushort Port, string Address, object?[] Args)> ReceivedMessages = [];

    public string GetDefaultIp()
        => _config.Osc_Routing_TargetIp;
    public ushort GetDefaultPort()
        => _config.Osc_Routing_TargetPort;

    public async Task<bool> SendAsync(string ip, ushort port, string address, params object?[] args)
    {
        ReceivedMessages.Add((ip, port, address, args));
        return true;
    }

    public bool SendSync(string ip, ushort port, string address, params object?[] args)
    {
        ReceivedMessages.Add((ip, port, address, args));
        return true;
    }

    public void SendSyncFireAndForget(string ip, ushort port, string address, params object?[] args)
    {
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
}