using HoscyCore.Services.Audio;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests.AudioServiceTests;

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