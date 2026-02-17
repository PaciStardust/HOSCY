using HoscyCore.Services.Core;

namespace HoscyCore.Services.Osc.SendReceive;

/// <summary>
/// Interface for an OSC Sender
/// </summary>
public interface IOscSendService : IService
{
    public string GetDefaultIp();
    public ushort GetDefaultPort();

    public void SendToDefaultSyncFireAndForget(string address, params object?[] args);
    public void SendSyncFireAndForget(string ip, ushort port, string address, params object?[] args);
    public bool SendToDefaultSync(string address, params object?[] args);
    public bool SendSync(string ip, ushort port, string address, params object?[] args);
    public Task<bool> SendToDefaultAsync(string address, params object?[] args);
    public Task<bool> SendAsync(string ip, ushort port, string address, params object?[] args);
}