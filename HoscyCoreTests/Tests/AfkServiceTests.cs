using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Misc;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class AfkServiceTests : TestBaseForService<AfkServiceTests>
{
    private AfkService _afk = null!;
    private readonly ConfigModel _config = new();
    private readonly MockOutputManagerService _output = new();

    protected override void OneTimeSetupExtra()
    {
        var afk = new AfkService(_config, _output, _logger);
        afk.Start();
        _afk = afk;
        AssertServiceStarted(_afk);
    }

    const string START = "AFKNOW";
    const string RETURN = "AFKBACK";
    const string STATUS = "AFK FOR";

    protected override void SetupExtra()
    {
        if (_afk.GetCurrentStatus() != ServiceStatus.Stopped)
            _afk.StopAfk();
        else 
            _afk.Start();

        _output.Clear();

        _config.Afk_StartText = START;
        _config.Afk_StopText = RETURN;
        _config.Afk_StatusText = STATUS;
        _config.Afk_TimesDisplayedBeforeDoublingInterval = 1;
        _config.Afk_ShowDuration = true;
        _config.Afk_BaseDurationDisplayIntervalSeconds = 5f;
    }

    [Test]
    public void TestShowOff()
    {
        _config.Afk_ShowDuration = false;

        Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");
        _afk.StartAfk();
        Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started because setting off");
    }

    [Test]
    public void TestShowOn()
    {
        Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");
        _afk.StartAfk();

        Assert.Multiple(() =>
        {
            Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Processing), "Not processing");
            Assert.That(_output.Notifications, Has.Count.EqualTo(1), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });
        Assert.That(_output.Notifications[0].Message, Is.EqualTo(START), "Wrong afk start message");

        Thread.Sleep(4000);
        
        Assert.Multiple(() =>
        {
            Assert.That(_output.Notifications, Has.Count.EqualTo(1), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });

        Thread.Sleep(2000);
        Assert.Multiple(() =>
        {
            Assert.That(_output.Notifications, Has.Count.EqualTo(2), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });
        Assert.That(_output.Notifications[1].Message, Does.StartWith(STATUS), "Wrong afk status message");


        Thread.Sleep(6000);
        Assert.Multiple(() =>
        {
            Assert.That(_output.Notifications, Has.Count.EqualTo(3), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });

        Thread.Sleep(6000);
        Assert.Multiple(() =>
        {
            Assert.That(_output.Notifications, Has.Count.EqualTo(3), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
            Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Processing), "Not procesing");
        });
        Assert.That(_output.Notifications[2].Message, Does.StartWith(STATUS), "Wrong afk status message");

        _afk.StopAfk();
        Assert.Multiple(() =>
        {
            Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");
            Assert.That(_output.Notifications, Has.Count.EqualTo(4), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });
        Assert.That(_output.Notifications[3].Message, Is.EqualTo(RETURN), "Wrong afk stop message");
    }

    [Test]
    public void StoppageTest()
    {
        Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");
        _afk.StartAfk();

        Assert.Multiple(() =>
        {
            Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Processing), "Not processing");
            Assert.That(_output.Notifications, Has.Count.EqualTo(1), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });
        Assert.That(_output.Notifications[0].Message, Is.EqualTo(START), "Wrong afk start message");

        Thread.Sleep(6000);
        
        Assert.Multiple(() =>
        {
            Assert.That(_output.Notifications, Has.Count.EqualTo(2), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });
        Assert.That(_output.Notifications[1].Message, Does.StartWith(STATUS), "Wrong afk status message");

        _afk.Stop();
        Assert.Multiple(() =>
        {
            Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");
            Assert.That(_output.Notifications, Has.Count.EqualTo(3), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });
        Assert.That(_output.Notifications[2].Message, Is.EqualTo(RETURN), "Wrong afk stop message");

        _afk.Start();
        Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");

        Thread.Sleep(6000);
        
        Assert.Multiple(() =>
        {
            Assert.That(_output.Notifications, Has.Count.EqualTo(3), "Not correct notification count");
            Assert.That(_output.Messages, Is.Empty, "Not correct message count");
        });
    }

    protected override void OneTimeTearDownExtra()
    {
        _afk.Stop();
        AssertServiceStopped(_afk);
    }
}