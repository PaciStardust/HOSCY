using HoscyCore.Services.DependencyCore;

namespace HoscyCoreTests.Utils;

public abstract class TestBaseForService<T> : TestBase<T>
{
    [Test]
    public void OnlyForStartStop() {}

    protected static void AssertServiceStarted(IStartStopService startStopService)
    {
        var status = startStopService.GetCurrentStatus();
        Assert.That(status, Is.AnyOf(ServiceStatus.Started, ServiceStatus.Processing), "Service status not started");
    }

    protected static void AssertServiceStopped(IStartStopService startStopService)
    {
        var status = startStopService.GetCurrentStatus();
        Assert.That(status, Is.AnyOf(ServiceStatus.Stopped), "Service status not stopped");
    }
}