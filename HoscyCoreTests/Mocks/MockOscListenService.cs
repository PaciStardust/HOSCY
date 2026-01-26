using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Osc.SendReceive;

namespace HoscyCoreTests.Mocks;

public class MockOscListenService : IOscListenService
{
    public bool Running { get; private set; } = false;
    public int Port { get; set; }
    
    public ServiceStatus GetCurrentStatus()
    {
        return Running 
            ? GetFaultIfExists() is null
                ? ServiceStatus.Processing 
                : ServiceStatus.Faulted
            : ServiceStatus.Stopped;
    }

    public Exception? GetFaultIfExists()
    {
        return null;
    }

    public int? GetPort()
    {
        return Running ? Port : null;
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    public void Start()
    {
        Running = true;
    }

    public void Stop()
    {
        Running = false;
    }
}