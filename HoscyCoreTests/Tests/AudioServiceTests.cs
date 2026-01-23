using HoscyCore.Services.Audio;
using HoscyCore.Services.DependencyCore;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class AudioServiceTests : TestBase<AudioServiceTests>
{
    private IAudioService? _audioService;

    [Test, Order(int.MinValue)]
    public void Start()
    {
        _audioService = new AudioService(_logger);
        _audioService.Start();

        var status = _audioService.GetCurrentStatus();
        Assert.That(status, Is.AnyOf(ServiceStatus.Started, ServiceStatus.Processing), "Service status not started");
    }

    [Test, Order(int.MaxValue)]
    public void Stop()
    {
        Assert.That(_audioService, Is.Not.Null, "AudioService is null!");
        var status = _audioService.GetCurrentStatus();
        Assert.That(status, Is.AnyOf(ServiceStatus.Started, ServiceStatus.Processing), "Service status not started");
        _audioService.Stop();
        status = _audioService.GetCurrentStatus();
        Assert.That(status, Is.EqualTo(ServiceStatus.Stopped), "Service status not stopped");
    }
}