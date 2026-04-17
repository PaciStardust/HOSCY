using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Osc.SendReceive;

/// <summary>
/// Interface for an OSC Sender
/// </summary>
public interface IOscSendService : IStartStopService
{
    public string GetDefaultIp();
    public ushort GetDefaultPort();

    public void SendToDefaultSyncFireAndForget(string address, params object?[] args);
    public void SendSyncFireAndForget(string ip, ushort port, string address, params object?[] args);
    public Res SendToDefaultSync(string address, params object?[] args);
    public Res SendSync(string ip, ushort port, string address, params object?[] args);
    public Task<Res> SendToDefaultAsync(string address, params object?[] args);
    public Task<Res> SendAsync(string ip, ushort port, string address, params object?[] args);
}