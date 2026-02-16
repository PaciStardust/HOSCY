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

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SimpleStartStopRestartTest(_audio, false, restartNotStart, doAgain);
    }
}

public class AudioServiceFunctionTests : TestBase<AudioServiceFunctionTests>
{
    private AudioService _audioService = null!;

    protected override void OneTimeSetupExtra()
    {
        _audioService = new(_logger);
        _audioService.Start();
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
    }
}