using HoscyCore.Services.Audio;
using HoscyCore.Services.DependencyCore;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class AudioServiceTests : TestBaseForService<AudioServiceTests>
{
    private AudioService _audioService = null!;

    protected override void OneTimeSetupExtra()
    {
        _audioService = new AudioService(_logger);
        _audioService.Start();

        var status = _audioService.GetCurrentStatus();
        Assert.That(status, Is.AnyOf(ServiceStatus.Started, ServiceStatus.Processing), "Service status not started");
    }

    [Test, Order(int.MaxValue)]
    public void FinalTest()
    {
        var status = _audioService.GetCurrentStatus();
        Assert.That(status, Is.AnyOf(ServiceStatus.Started, ServiceStatus.Processing), "Service status not started");
    }

    protected override void OneTimeTearDownExtra()
    {
        _audioService.Stop();
        var status = _audioService.GetCurrentStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped), "Service status not stopped");
    }
}