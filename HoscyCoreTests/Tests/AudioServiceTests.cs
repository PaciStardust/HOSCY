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
        dev.AssertOk();
    }

    [Test]
    public void GetPlaybacksTest()
    {
        var dev = _audioService.GetPlaybackDevices();
        dev.AssertOk();
    }

    [TestCase(true), TestCase(false)]
    public void GetCaptureDeviceTest(bool setDevName)
    {
        var deviceResult = _audioService.GetPlaybackDevices();
        deviceResult.AssertOk();

        var devices = deviceResult.Value!;
        if (devices.Length == 0)
            Assert.Inconclusive("Could not locate audio device for test");

        if (setDevName)
        {
            _config.Audio_CurrentMicrophoneName = devices[0].Name;
        }

        var captureResult = _audioService.CreateCaptureDevice();
        captureResult.AssertOk();
        var capture = captureResult.Value!;

        var dataReceived = false;
        capture.OnAudioProcessed += new((_, __) => dataReceived = true);

        capture.Start();
        Thread.Sleep(200); // To make sure it actually triggers
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dataReceived, Is.True);
            Assert.That(capture.IsRunning, Is.True);
        }

        capture.Stop();
        Assert.That(capture.IsRunning, Is.False);

        capture.Dispose();
    }

    [TestCase(true), TestCase(false)]
    public void GetCaptureDeviceProxyTest(bool setDevName)
    {
        var deviceResult = _audioService.GetPlaybackDevices();
        deviceResult.AssertOk();

        var devices = deviceResult.Value!;
        if (devices.Length == 0)
            Assert.Inconclusive("Could not locate audio device for test");

        if (setDevName)
        {
            _config.Audio_CurrentMicrophoneName = devices[0].Name;
        }

        var captureResult = _audioService.CreateCaptureDeviceProxy();
        captureResult.AssertOk();
        var capture = captureResult.Value!;

        var dataReceived = false;
        capture.OnAudioProcessed += new((_, __) => dataReceived = true);

        capture.Start();
        Thread.Sleep(200); // To make sure it actually triggers
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dataReceived, Is.True);
            Assert.That(capture.IsStarted, Is.True);
        }

        capture.Stop();
        Assert.That(capture.IsStarted, Is.False);

        capture.Dispose();
    }

    protected override void OneTimeTearDownExtra()
    {
        _audioService.Stop().AssertOk();
    }
}