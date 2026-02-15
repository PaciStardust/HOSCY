using HoscyCore.Services.Audio;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests.AudioServiceTests;

public class AudioServiceFunctionTests : TestBase<AudioServiceFunctionTests>
{
    private AudioService _audioService = null!;

    protected override void OneTimeSetupExtra()
    {
        _audioService = new(_logger);
        _audioService.Start();
        AssertServiceProcessing(_audioService);
    }

    [Test]
    public void GetCapturesTest()
    {
        var dev = _audioService.GetCaptureDevices();
        Assert.That(dev, Is.Not.Null);
    }

    [Test]
    public void GetPlaybacksTest()
    {
        var dev = _audioService.GetPlaybackDevices();
        Assert.That(dev, Is.Not.Null);
    }

    protected override void OneTimeTearDownExtra()
    {
        _audioService.Stop();
        AssertServiceStopped(_audioService);
    }
}