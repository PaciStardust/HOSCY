using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Misc;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.AfkServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

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
    public void DoubleStartTest()
    {
        _afk.Start();
        AssertServiceStarted(_afk);

        _afk.Stop();

        _afk.Start();
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

public class AfkServiceFunctionTests : TestBase<AfkServiceFunctionTests>
{
    private AfkService _afk = null!;
    private readonly ConfigModel _config = new();
    private readonly MockOutputManagerService _output = new();

    protected override void OneTimeSetupExtra()
    {
        var afk = new AfkService(_config, _output, _logger);
        afk.Start();
        _afk = afk;
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
    public void ShowOffTest()
    {
        _config.Afk_ShowDuration = false;

        Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");
        _afk.StartAfk();
        Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started because setting off");
    }

    [Test]
    public void ShowOnTest()
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

    [Test]
    public void StartWhileRunningTest()
    {
        Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");
        _afk.StartAfk();

        Assert.Multiple(() =>
        {
            Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Processing), "Not processing");
            Assert.That(_output.Notifications, Has.Count.EqualTo(1), "Not correct notification count");
        });
        Assert.That(_output.Notifications[0].Message, Is.EqualTo(START), "Wrong afk start message");

        _afk.StartAfk();
        Assert.Multiple(() =>
        {
            Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Processing), "Not processing");
            Assert.That(_output.Notifications, Has.Count.EqualTo(1), "Not correct notification count");
        });

        _afk.StopAfk();
        Assert.Multiple(() =>
        {
            Assert.That(_afk.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Not started");
            Assert.That(_output.Notifications, Has.Count.EqualTo(2), "Not correct notification count");
        });
        Assert.That(_output.Notifications[1].Message, Is.EqualTo(RETURN), "Wrong afk stop message");
    }

    protected override void OneTimeTearDownExtra()
    {
        _afk.Stop();
    }
}