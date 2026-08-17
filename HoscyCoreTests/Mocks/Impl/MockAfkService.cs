using HoscyCore.Services.Afk;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockAfkService : MockStartStopServiceBase, IAfkService
{
    public bool AfkRunning { get; private set; } = false;

    public event Action<bool> OnAfkStatusChanged = delegate { };
    public bool GetAfkStatus()
    {
        return AfkRunning;
    }

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