using HoscyCore.Services.Misc;

namespace HoscyCoreTests.Mocks;

public class MockAfkService : MockStartStopServiceBase, IAfkService
{
    public bool AfkRunning { get; private set; } = false;
    public void StartAfk()
    {
        AfkRunning = true;
    }
    public override void Stop()
    {
        StopAfk();
        base.Stop();
    }
    public void StopAfk()
    {
        AfkRunning = false;
    }
}