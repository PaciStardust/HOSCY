using HoscyCore.Services.Osc.SendReceive;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockOscListenService : MockStartStopServiceBase, IOscListenService
{
    public int Port { get; set; }
    public Res<int> GetPort()
        => ResC.TOk(Port);
}