using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.AudioServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class AudioServiceStartupTests : TestBase<AudioServiceStartupTests>
{
    private ConfigModel _config = null!;
    private AudioService _audio = null!;

    protected override void SetupExtra()
    {
        _config = new();
        _audio = new(_logger, _config);
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SimpleStartStopRestartTest(_audio, false, restartNotStart, doAgain);
    }
}

public class AudioServiceFunctionTests : TestBase<AudioServiceFunctionTests>
{
    private readonly ConfigModel _config = new();
    private AudioService _audioService = null!;

    protected override void OneTimeSetupExtra()
    {
        var audioService = new AudioService(_logger, _config);
        audioService.Start().AssertOk();
        _audioService = audioService;
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
        _audioService.Stop().AssertOk();
    }
}