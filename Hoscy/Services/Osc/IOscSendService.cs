using System.Threading.Tasks;

namespace Hoscy.Services.Osc;

/// <summary>
/// Interface for an OSC Sender
/// </summary>
public interface IOscSendService
{
    public bool SendSync(string address, params object?[] args);
    public bool SendSync(string ip, ushort port, string address, params object?[] args);
    public Task<bool> SendAsync(string address, params object?[] args);
    public Task<bool> SendAsync(string ip, ushort port, string address, params object?[] args);
}