using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Misc;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests.AfkServiceTests;

public class AfkServiceStartupTests : TestBase<AfkServiceStartupTests>
{
    private ConfigModel _config = null!;
    private MockOutputManagerService _output = null!;

    private AfkService _afk = null!;

    protected override void SetupExtra()
    {
        _config = new()
        {
            Afk_TimesDisplayedBeforeDoublingInterval = 1,
            Afk_ShowDuration = true,
            Afk_BaseDurationDisplayIntervalSeconds = 5f
        };

        _output = new();

        _afk = new(_config, _output, _logger);
    }

    [Test]
    public void StartStopRestartTest()
    {
        _afk.Start();
        AssertServiceStarted(_afk);

        _afk.Restart();
        AssertServiceStarted(_afk);

        _afk.Stop();

        Assert.That(_output.Notifications, Is.Empty);
        Thread.Sleep(6000);
        Assert.That(_output.Notifications, Is.Empty);
    }

    [Test]
    public void StartStopRestartWithAfkTest()
    {
        _afk.Start();
        AssertServiceStarted(_afk);

        _afk.StartAfk();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_afk);
            Assert.That(_output.Notifications, Has.Count.EqualTo(1));
        }

        _afk.Restart();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceStarted(_afk);
            Assert.That(_output.Notifications, Has.Count.EqualTo(2));
        }
        Thread.Sleep(6000);
        Assert.That(_output.Notifications, Has.Count.EqualTo(2));

        _afk.StartAfk();
        using (Assert.EnterMultipleScope())
        {
            AssertServiceProcessing(_afk);
            Assert.That(_output.Notifications, Has.Count.EqualTo(3));
        }

        _afk.Stop();

        Assert.That(_output.Notifications, Has.Count.EqualTo(4));
        Thread.Sleep(6000);
        Assert.That(_output.Notifications, Has.Count.EqualTo(4));
    }
}