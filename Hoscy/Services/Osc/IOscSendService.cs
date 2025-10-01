namespace Hoscy.Services.Osc;

public interface IOscSendService
{
    public void Send(string address, params object?[] args);
    public void Send(string ip, ushort port, string address, params object?[] args);
}