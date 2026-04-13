using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Osc.SendReceive;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.OscSendAndListenServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class OscSendAndListenServiceStartupTests : TestBase<OscSendAndListenServiceStartupTests>
{
    private ConfigModel _config = null!;
    private MockBackToFrontNotifyService _notify = null!;
    private MockOscMessageHandlingService _handler = null!;
    private MockOscRelayService _relay = null!;

    private OscListenService _listen = null!;

    protected override void SetupExtra()
    {
        _config = new();
        _notify = new();
        _handler = new();
        _relay = new();

        _listen = new(_config, _logger, _notify, _handler, _relay);
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SimpleStartStopRestartTest(_listen, false, restartNotStart, doAgain);
    }
}

public class OscSendAndListenServiceFunctionTests : TestBase<OscSendAndListenServiceFunctionTests>
{
    private readonly ConfigModel _config = new();
    private readonly MockBackToFrontNotifyService _notify = new();
    private readonly MockOscMessageHandlingService _handler = new();
    private readonly MockOscRelayService _relay = new();

    private OscListenService _listen = null!;
    private OscSendService _send = null!;

    protected override void OneTimeSetupExtra()
    {
        _send = new(_logger, _config, _notify);

        var listen = new OscListenService(_config, _logger, _notify, _handler, _relay);
        listen.Start().AssertOk();
        _listen = listen;
    }

    protected override void SetupExtra()
    {
        _notify.Notifications.Clear();
        _handler.ReceivedMessages.Clear();
        _relay.Clear();

        _config.Osc_Routing_TargetIp = "127.0.0.1";
        _config.Osc_Routing_TargetPort = 8642;
        _config.Osc_Relay_IgnoreIfHandled = true;

        _config.Osc_Routing_ListenPort = 8642;
        _listen.Restart().AssertOk();

        var portResult = _listen.GetPort();
        portResult.AssertOk();
        Assert.That(portResult.Value!, Is.EqualTo(_config.Osc_Routing_ListenPort));
    }

    [Test]
    public async Task SendImplicitIpTestAsync()
    {
        var portResult = _listen.GetPort();
        portResult.AssertOk();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_config.Osc_Routing_TargetPort, Is.EqualTo(_config.Osc_Routing_ListenPort));
            Assert.That(_config.Osc_Routing_ListenPort, Is.EqualTo(portResult.Value));

            Assert.That(_send.GetDefaultIp(), Is.EqualTo(_config.Osc_Routing_TargetIp));
            Assert.That(_send.GetDefaultPort(), Is.EqualTo(_config.Osc_Routing_TargetPort));
        }

        var result = _send.SendToDefaultSync("/sync", true);
        await Task.Delay(5);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_notify.Notifications, Is.Empty);
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(1));
        }
        Assert.That(_handler.ReceivedMessages[0].Address, Is.EqualTo("/sync"));

        result = await _send.SendToDefaultAsync("/async", true);
        await Task.Delay(5);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_notify.Notifications, Is.Empty);
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(2));
        }
        Assert.That(_handler.ReceivedMessages[1].Address, Is.EqualTo("/async"));

        _send.SendToDefaultSyncFireAndForget("/forget", true);
        await Task.Delay(5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_notify.Notifications, Is.Empty);
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(3));
        }
        Assert.That(_handler.ReceivedMessages[2].Address, Is.EqualTo("/forget"));

        _config.Osc_Routing_TargetPort++;

        result = _send.SendToDefaultSync("/sync2", true);
        var result2 = await _send.SendToDefaultAsync("/async2", true);
        _send.SendToDefaultSyncFireAndForget("/forget2", true);
        await Task.Delay(5);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            result2.AssertOk();
            Assert.That(_notify.Notifications, Is.Empty);
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(3));
        }

        Assert.That(_notify.Notifications, Is.Empty);
    }

    [Test]
    public async Task SendExplicitIpTestAsync()
    {
        _config.Osc_Routing_TargetPort++;
        var portResult = _listen.GetPort();
        portResult.AssertOk();

        var listenPort = portResult.Value.ConvertToUshort();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_config.Osc_Routing_TargetPort, Is.Not.EqualTo(_config.Osc_Routing_ListenPort));
            Assert.That(_config.Osc_Routing_ListenPort, Is.EqualTo(listenPort));
        }

        var result = _send.SendSync(_config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort, "/sync", true);
        var result2 = await _send.SendAsync(_config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort, "/async", true);
        _send.SendSyncFireAndForget(_config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort, "/forget", true);
        await Task.Delay(5);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            result2.AssertOk();
            Assert.That(_notify.Notifications, Is.Empty);
            Assert.That(_handler.ReceivedMessages, Is.Empty);
        }

        result = _send.SendSync(_config.Osc_Routing_TargetIp, listenPort, "/sync2", true);
        await Task.Delay(5);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_notify.Notifications, Is.Empty);
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(1));
        }
        Assert.That(_handler.ReceivedMessages[0].Address, Is.EqualTo("/sync2"));

        result = await _send.SendAsync(_config.Osc_Routing_TargetIp, listenPort, "/async2", true);
        await Task.Delay(5);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_notify.Notifications, Is.Empty);
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(2));
        }
        Assert.That(_handler.ReceivedMessages[1].Address, Is.EqualTo("/async2"));

        _send.SendSyncFireAndForget(_config.Osc_Routing_TargetIp, listenPort, "/forget2", true);
        await Task.Delay(5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_notify.Notifications, Is.Empty);
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(3));
        }
        Assert.That(_handler.ReceivedMessages[2].Address, Is.EqualTo("/forget2"));

        Assert.That(_notify.Notifications, Is.Empty);
    }

    [Test]
    public void SendReceiveParameterTest()
    {
        object[] parameters = [ true, false, 1, -1, 89978787, string.Empty, "this is a \n test", 0, 1.1f, -0.0001f, 1727849849.3f ];
        var result = _send.SendToDefaultSync("/this/is/a/parameter_test", parameters);

        Thread.Sleep(10);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handler.ReceivedMessages[0].Address, Is.EqualTo("/this/is/a/parameter_test"));
            Assert.That(_handler.ReceivedMessages[0].Arguments, Has.Length.EqualTo(parameters.Length));
            for (int i = 0; i < parameters.Length; i++)
                Assert.That(_handler.ReceivedMessages[0].Arguments[i], Is.EqualTo(parameters[i]));
        }

        Assert.That(_notify.Notifications, Is.Empty);
    }

    [Test]
    public void SenderErrorHandlingTest()
    {
        var result = _send.SendSync("notAnIp", _config.Osc_Routing_TargetPort, "/test", false);
        result.AssertFail();

        result = _send.SendSync(_config.Osc_Routing_TargetIp, 0, "/test", false);
        result.AssertFail();

        result = _send.SendSync(_config.Osc_Routing_TargetIp, _config.Osc_Routing_TargetPort, "/test", this);
        result.AssertFail();

        Thread.Sleep(10);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_notify.Notifications, Has.Count.EqualTo(3));
            Assert.That(_handler.ReceivedMessages, Is.Empty);
        }
    }

    [Test]
    public void ListenCorrectHandlingTest()
    {
        _config.Osc_Relay_IgnoreIfHandled = true;
        _handler.DoesHandle = false;

        var result = _send.SendToDefaultSync("/test", true);
        Thread.Sleep(10);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_notify.Notifications, Has.Count.EqualTo(0));
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(1));
            Assert.That(_relay.ReceivedMessages, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            var hMessage = _handler.ReceivedMessages[0];
            var rMessage = _relay.ReceivedMessages[0];

            Assert.That(hMessage.Address, Is.EqualTo("/test").And.EqualTo(rMessage.Address));
            Assert.That(hMessage.Arguments[0], Is.True.And.EqualTo(rMessage.Arguments[0]));
        }

        _handler.DoesHandle = true;

        result = _send.SendToDefaultSync("/test2", true);
        Thread.Sleep(10);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_notify.Notifications, Has.Count.EqualTo(0));
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(2));
            Assert.That(_relay.ReceivedMessages, Has.Count.EqualTo(1));
        }
        using (Assert.EnterMultipleScope())
        {
            var hMessage = _handler.ReceivedMessages[1];

            Assert.That(hMessage.Address, Is.EqualTo("/test2"));
            Assert.That(hMessage.Arguments[0], Is.True);
        }

        _config.Osc_Relay_IgnoreIfHandled = false;

        result = _send.SendToDefaultSync("/test3", true);
        Thread.Sleep(10);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_notify.Notifications, Has.Count.EqualTo(0));
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(3));
            Assert.That(_relay.ReceivedMessages, Has.Count.EqualTo(2));
        }
        using (Assert.EnterMultipleScope())
        {
            var hMessage = _handler.ReceivedMessages[2];
            var rMessage = _relay.ReceivedMessages[1];

            Assert.That(hMessage.Address, Is.EqualTo("/test3").And.EqualTo(rMessage.Address));
            Assert.That(hMessage.Arguments[0], Is.True.And.EqualTo(rMessage.Arguments[0]));
        }

        _handler.DoesHandle = false;

        result = _send.SendToDefaultSync("/test4", true);
        Thread.Sleep(10);

        using (Assert.EnterMultipleScope())
        {
            result.AssertOk();
            Assert.That(_notify.Notifications, Has.Count.EqualTo(0));
            Assert.That(_handler.ReceivedMessages, Has.Count.EqualTo(4));
            Assert.That(_relay.ReceivedMessages, Has.Count.EqualTo(3));
        }
        using (Assert.EnterMultipleScope())
        {
            var hMessage = _handler.ReceivedMessages[3];
            var rMessage = _relay.ReceivedMessages[2];

            Assert.That(hMessage.Address, Is.EqualTo("/test4").And.EqualTo(rMessage.Address));
            Assert.That(hMessage.Arguments[0], Is.True.And.EqualTo(rMessage.Arguments[0]));
        }
    }

    protected override void OneTimeTearDownExtra()
    {
        _listen.Stop().AssertOk();
    }
}