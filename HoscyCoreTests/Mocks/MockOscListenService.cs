using HoscyCore.Services.Osc.SendReceive;

namespace HoscyCoreTests.Mocks;

public class MockOscListenService : MockStartStopServiceBase, IOscListenService
{
    public int Port { get; set; }
    public int? GetPort()
        => Port;
}