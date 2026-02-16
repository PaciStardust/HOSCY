using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Osc.Relay;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;
using LucHeart.CoreOSC;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.OscRelayServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class OscRelayServiceStartupTests : TestBase<OscRelayServiceStartupTests>
{
    private ConfigModel _config = null!;
    private MockBackToFrontNotifyService _notify = null!;
    private MockOscSendService _sender = null!;

    private OscRelayService _relay = null!;

    protected override void SetupExtra()
    {
        _config = new();
        _notify = new();
        _sender = new(_config);

        _relay = new(_logger, _config, _sender, _notify);
    }

    [TestCase(true), TestCase(false)]
    public void StartStopRestartTest(bool restartNotStart)
    {
        AssertServiceStopped(_relay);
        
        _relay.Start();
        AssertServiceStarted(_relay);

        _config.Osc_Relay_Filters.Add(new()
        {
            Enabled = true,
            Ip = "127.0.0.1",
            Port = 42069,
            Filters = ["A"]
        });

        if (restartNotStart)
            _relay.Restart();
        else
            _relay.Start();
            
        AssertServiceProcessing(_relay);

        _relay.Stop();
        AssertServiceStopped(_relay);
    }
}

public class OscRelayServiceFunctionTests : TestBase<OscRelayServiceFunctionTests>
{
    private readonly ConfigModel _config = new();
    private readonly MockBackToFrontNotifyService _notify = new();
    private MockOscSendService _sender = null!;
    private OscRelayService _relay = null!;

    protected override void OneTimeSetupExtra()
    {
        _sender = new(_config);
        _relay = new(_logger, _config, _sender, _notify);

        _relay.Start();
    }

    protected override void SetupExtra()
    {
        _sender.Clear();
        _config.Osc_Relay_Filters.Clear();
        _relay.Restart();
        AssertServiceStarted(_relay);
    }

    [Test]
    public void TestInvalidFilters()
    {
        _sender.BannedIps.Add("0.0.0.0");
        
        _config.Osc_Relay_Filters.AddRange([
            new() {
                Name = "Test1",
                Ip = string.Empty,
                Port = _config.Osc_Routing_TargetPort,
                Filters = [], BlacklistMode = true
            },
            new() {
                Name = "Test2",
                Ip = _config.Osc_Routing_TargetIp,
                Port = ushort.MinValue,
                Filters = [], BlacklistMode = true
            },
            new() {
                Name = "Test3",
                Ip = "0.0.0.0",
                Port = _config.Osc_Routing_TargetPort,
                Filters = [], BlacklistMode = true
            },
            new() {
                Name = "Test4",
                Ip = "1.1.1.1",
                Port = _config.Osc_Routing_TargetPort,
                Filters = [], BlacklistMode = true
            },
            new() {
                Name = "Test5",
                Ip = "1.1.1.1",
                Port = _config.Osc_Routing_TargetPort,
                Filters = [], BlacklistMode = true,
                Enabled = false
            }
        ]);
        _relay.Restart();

        var invalidFilters = _relay.GetInvalidFilterNames();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalidFilters, Has.Length.EqualTo(_config.Osc_Relay_Filters.Count - 2));
            Assert.That(invalidFilters, Does.Not.Contain("Test4"));
            Assert.That(invalidFilters, Does.Not.Contain("Test5"));
            Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(1));
        }

        var message = new OscMessage("/test", true);
        _relay.HandleRelay(message);

        Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(2));
        
        var (Ip, Port, Address, Args) = _sender.ReceivedMessages[1];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Address, Is.EqualTo("/test"));
            Assert.That(Args[0], Is.True);
            Assert.That(Ip, Is.EqualTo("1.1.1.1"));
            Assert.That(Port, Is.EqualTo(_config.Osc_Routing_TargetPort));
        }
    }

    [Test]
    public void TestRelay()
    {
        _config.Osc_Relay_Filters.AddRange([
            new() {
                Name = "Test1",
                Ip = "1.1.1.1",
                Port = 11111,
                Filters = [], BlacklistMode = true
            },
            new() {
                Name = "Test2",
                Ip = "2.2.2.2",
                Port = 22222,
                Filters = [], BlacklistMode = true
            },
            new() {
                Name = "Test3",
                Ip = "3.3.3.3",
                Port = 33333,
                Filters = ["/"], BlacklistMode = true
            },
            new() {
                Name = "Test4",
                Ip = "4.4.4.4",
                Port = 44444,
                Enabled = false
            }
        ]);

        _relay.Restart();
        Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(3));
        _sender.Clear();

        var testMessage = new OscMessage("/osctest", true);
        _relay.HandleRelay(testMessage);

        Assert.That(_sender.ReceivedMessages, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sender.ReceivedMessages[0].Address, Is.EqualTo("/osctest"));
            Assert.That(_sender.ReceivedMessages[0].Args[0], Is.True);
            Assert.That(_sender.ReceivedMessages[0].Ip, Is.EqualTo("1.1.1.1"));
            Assert.That(_sender.ReceivedMessages[0].Port, Is.EqualTo(11111));

            Assert.That(_sender.ReceivedMessages[1].Address, Is.EqualTo("/osctest"));
            Assert.That(_sender.ReceivedMessages[1].Args[0], Is.True);
            Assert.That(_sender.ReceivedMessages[1].Ip, Is.EqualTo("2.2.2.2"));
            Assert.That(_sender.ReceivedMessages[1].Port, Is.EqualTo(22222));
        }
    }

    protected override void OneTimeTearDownExtra()
    {
        _relay.Stop();
    }
}