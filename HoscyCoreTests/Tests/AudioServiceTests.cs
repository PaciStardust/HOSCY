using HoscyCore.Services.Audio;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class AudioServiceTests : TestBaseForService<AudioServiceTests>
{
    private AudioService _audioService = null!;

    protected override void OneTimeSetupExtra()
    {
        _audioService = new AudioService(_logger);
        _audioService.Start();

        AssertServiceProcessing(_audioService);
    }

    [Test, Order(int.MaxValue)]
    public void FinalTest()
    {
        AssertServiceProcessing(_audioService);
    }

    protected override void OneTimeTearDownExtra()
    {
        _audioService.Stop();
        AssertServiceStopped(_audioService);
    }
}