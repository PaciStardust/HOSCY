using HoscyCore.Services.Afk;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockAfkService : MockStartStopServiceBase, IAfkService
{
    public bool AfkRunning { get; private set; } = false;
    public void StartAfk()
    {
        AfkRunning = true;
    }
    public override Res Stop()
    {
        StopAfk();
        return base.Stop();
    }
    public void StopAfk()
    {
        AfkRunning = false;
    }
}