using HoscyCore.Services.Osc.MessageHandling;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;
using LucHeart.CoreOSC;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.OscMessageHandlingServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class OscMessageHandlingServiceStartupTests : TestBase<OscMessageHandlingServiceStartupTests>
{
    private OscMessageHandlingService _handlingService = null!;
    private MockContainerBulkLoader<IOscMessageHandler> _bulkLoader = null!;
    private MockOscMessageHandlerA _mockHandlerA = null!;

    protected override void SetupExtra()
    {
        _mockHandlerA = new();
        _bulkLoader = new(() => [_mockHandlerA]);

        _handlingService = new(_logger, _bulkLoader);
    }

    [Test]
    public void StartStopTest()
    {
        AssertServiceStopped(_handlingService);
        
        _handlingService.Start();
        AssertServiceProcessing(_handlingService);

        _handlingService.Stop();
        AssertServiceStopped(_handlingService);

        _bulkLoader = new(() => []);
        _handlingService = new(_logger, _bulkLoader);
        AssertServiceStopped(_handlingService);

        _handlingService.Start();
        AssertServiceStarted(_handlingService);

        _handlingService.Stop();
        AssertServiceStopped(_handlingService);
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SimpleStartStopRestartTest(_handlingService, false, restartNotStart, doAgain);
    }
}

public class OscMessageHandlingServiceFunctionTests : TestBase<OscMessageHandlingServiceFunctionTests>
{
    private OscMessageHandlingService _handlingService = null!;
    private MockContainerBulkLoader<IOscMessageHandler> _bulkLoader = null!;
    private readonly MockOscMessageHandlerA _mockHandlerA = new();
    private readonly MockOscMessageHandlerB _mockHandlerB = new();

    protected override void OneTimeSetupExtra()
    {
        _bulkLoader = new(() =>
        {
            return [_mockHandlerA, _mockHandlerB];
        });

        _handlingService = new(_logger, _bulkLoader);
        _handlingService.Start();
    }

    [Test]
    public void TestHandling()
    {
        _mockHandlerA.ReceivedMessages.Clear();
        _mockHandlerB.ReceivedMessages.Clear();

        // Message handled by first
        _mockHandlerA.ReturnValue = true;
        _mockHandlerB.ReturnValue = false;

        var oscMessage = new OscMessage("/test", true);
        var result = _handlingService.HandleMessage(oscMessage);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True, "Message should be handled");
            Assert.That(_mockHandlerA.ReceivedMessages, Has.Count.EqualTo(1), "First handler should receive one message");
            Assert.That(_mockHandlerB.ReceivedMessages, Is.Empty, "Second handler should not receive any messages");
        }
        Assert.That(_mockHandlerA.ReceivedMessages[0], Is.EqualTo(oscMessage));

        // Message handled by second
        _mockHandlerA.ReturnValue = false;
        _mockHandlerB.ReturnValue = true;

        oscMessage = new OscMessage("/test2", true);
        result = _handlingService.HandleMessage(oscMessage);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True, "Message should be handled");
            Assert.That(_mockHandlerA.ReceivedMessages, Has.Count.EqualTo(2), "First handler should receive a message");
            Assert.That(_mockHandlerB.ReceivedMessages, Has.Count.EqualTo(1), "Second handler should receive a messages");
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_mockHandlerA.ReceivedMessages[1], Is.EqualTo(oscMessage));
            Assert.That(_mockHandlerB.ReceivedMessages[0], Is.EqualTo(oscMessage));
        }

        // Message handled by neither
        _mockHandlerA.ReturnValue = false;
        _mockHandlerB.ReturnValue = false;

        oscMessage = new OscMessage("/test3", true);
        result = _handlingService.HandleMessage(oscMessage);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False, "Message should not be handled");
            Assert.That(_mockHandlerA.ReceivedMessages, Has.Count.EqualTo(3), "First handler should receive a message");
            Assert.That(_mockHandlerB.ReceivedMessages, Has.Count.EqualTo(2), "Second handler should receive a messages");
        }
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_mockHandlerA.ReceivedMessages[2], Is.EqualTo(oscMessage));
            Assert.That(_mockHandlerB.ReceivedMessages[1], Is.EqualTo(oscMessage));
        }
    }

    protected override void OneTimeTearDownExtra()
    {
        _handlingService.Stop();
    }
}