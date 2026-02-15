using HoscyCore.Services.Audio;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.AudioServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class AudioServiceStartupTests : TestBase<AudioServiceStartupTests>
{
    private AudioService _audio = null!;

    protected override void SetupExtra()
    {
        _audio = new(_logger);
    }

    [Test]
    public void StartStopRestartTest()
    {
        AssertServiceStopped(_audio);

        _audio.Start();
        AssertServiceProcessing(_audio);

        _audio.Restart();
        AssertServiceProcessing(_audio);

        _audio.Stop();
        AssertServiceStopped(_audio);
    }

    [Test]
    public void StartDoubleTest()
    {
        AssertServiceStopped(_audio);

        _audio.Start();
        AssertServiceProcessing(_audio);

        _audio.Start();
        AssertServiceProcessing(_audio);

        _audio.Stop();
        AssertServiceStopped(_audio);
    }
}

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