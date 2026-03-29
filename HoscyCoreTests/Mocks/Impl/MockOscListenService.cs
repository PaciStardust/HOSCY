using HoscyCore.Services.Osc.SendReceive;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockOscListenService : MockStartStopServiceBase, IOscListenService
{
    public int Port { get; set; }
    public int? GetPort()
        => Port;
}