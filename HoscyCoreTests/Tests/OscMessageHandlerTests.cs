using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Osc.MessageHandling.Handlers;
using HoscyCore.Services.Output.Core;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;
using LucHeart.CoreOSC;

namespace HoscyCoreTests.Tests;

public class OscMessageHandlerTests : TestBase<OscMessageHandlerTests>
{
    private readonly ConfigModel _config = new();

    [Test]
    public void TestAfkHandler()
    {
        var afkService = new MockAfkService();
        afkService.Start();
        var afkHandler = new AfkOscMessageHandler(_logger, afkService, _config);
        
        // Status sanity check
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afkService.Started, Is.True, "Afk Service should be started");
            Assert.That(afkService.AfkRunning, Is.False, "Afk Service should not be running");
            Assert.That(afkService.GetCurrentStatus(), Is.EqualTo(ServiceStatus.Started), "Afk Service status should be Started");
        }

        // Invalid address should not be handled
        var message = new OscMessage("/not/correct", true);
        AssertAfkMessage(message, afkHandler, afkService, false, false);

        // Invalid parameter should be handled but not change state
        message = new OscMessage(_config.Osc_Address_Game_Afk, "wawawa");
        AssertAfkMessage(message, afkHandler, afkService, true, false);

        // Correct address should be handled, second message should not change the state
        for (var i = 0; i < 2; i++)
        {
            message = new OscMessage(_config.Osc_Address_Game_Afk, true);
            AssertAfkMessage(message, afkHandler, afkService, true, true);
        }

        // Invalid parameter should be handled but not change state
        message = new OscMessage(_config.Osc_Address_Game_Afk, "wawawa");
        AssertAfkMessage(message, afkHandler, afkService, true, true);

        // Correct address should be handled, second message should not change the state
        for (var i = 0; i < 2; i++)
        {
            message = new OscMessage(_config.Osc_Address_Game_Afk, false);
            AssertAfkMessage(message, afkHandler, afkService, true, false);
        }
    }

    private void AssertAfkMessage(OscMessage message, AfkOscMessageHandler afkHandler, MockAfkService afkService, bool shouldBeHandled, bool shouldBeRunning)
    {
        var handled = afkHandler.HandleMessage(message);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, shouldBeHandled ? Is.True : Is.False, "Message should be handled");
            Assert.That(afkService.Started, Is.True, "Afk Service started state wrong");
            Assert.That(afkService.AfkRunning, shouldBeRunning ? Is.True : Is.False, "Afk Service running state wrong");
            Assert.That(afkService.GetCurrentStatus(), Is.EqualTo(shouldBeRunning ? ServiceStatus.Processing : ServiceStatus.Started), "Afk Service status wrong");
        }
    }

    [Test]
    public void TestCounterHandlerIncrease()
    {
        _config.Counters_List.Clear();
        var counterA = new CounterModel()
        {
            CooldownSeconds = 0,
            Enabled = true,
            Name = "CounterA",
            Parameter = "CounterA" 
        };
        var counterB = new CounterModel()
        {
            CooldownSeconds = 1,
            Enabled = true,
            Name = "CounterB",
            Parameter = "/testing/CounterB"
        };
        _config.Counters_List.AddRange([counterA, counterB]);
        _config.Counters_ShowNotification = true;

        var output = new MockOutputManagerService();
        var handler = new CounterOscMessageHandler(output, _config, _logger);

        // Values should be nothing
        using (Assert.EnterMultipleScope())
        {
            AssertCounter(counterA, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
            AssertCounter(counterB, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
        }

        //Incorrect counter should not be handled
        var handled = SendCounterOther(handler, "/testing/CounterC", true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.False, "Message should not be handled");
            AssertCounter(counterA, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
            AssertCounter(counterB, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
        }

        //Counter should not be handled with incorrect parameter or "false"
        handled = SendCounterOther(handler, counterA.FullParameter(), "test")
            || SendCounterOther(handler, counterB.FullParameter(), false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.False, "Messages should not be handled");
            AssertCounter(counterA, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
            AssertCounter(counterB, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
        }
        
        //Only one counter should increase
        var startUseA = DateTimeOffset.UtcNow;
        handled = SendCounterMessage(handler, counterA);
        var endUseA = DateTimeOffset.UtcNow;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True, "Message should be handled");
            AssertCounter(counterA, 1, startUseA, endUseA);
            AssertCounter(counterB, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);
        }

        var startUseB = DateTimeOffset.UtcNow;
        handled = SendCounterMessage(handler, counterB);
        var endUseB = DateTimeOffset.UtcNow;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True, "Message should be handled");
            AssertCounter(counterA, 1, startUseA, endUseA);
            AssertCounter(counterB, 1, startUseB, endUseB);
        }

        //Counter should respect timeout
        Thread.Sleep(1000);

        handled = SendCounterMessage(handler, counterA);
        startUseA = DateTimeOffset.UtcNow;
        handled &= SendCounterMessage(handler, counterA);
        endUseA = DateTimeOffset.UtcNow;

        startUseB = DateTimeOffset.UtcNow;
        handled &= SendCounterMessage(handler, counterB);
        endUseB = DateTimeOffset.UtcNow;
        handled &= SendCounterMessage(handler, counterB);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True, "All messages should be handled");
            AssertCounter(counterA, 3, startUseA, endUseA);
            AssertCounter(counterB, 2, startUseB, endUseB);
        }

        Thread.Sleep(1000);

        startUseA = DateTimeOffset.UtcNow;
        handled = SendCounterMessage(handler, counterA);
        endUseA = DateTimeOffset.UtcNow;

        startUseB = DateTimeOffset.UtcNow;
        handled &= SendCounterMessage(handler, counterB);
        endUseB = DateTimeOffset.UtcNow;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True, "All messages should be handled");
            AssertCounter(counterA, 4, startUseA, endUseA);
            AssertCounter(counterB, 3, startUseB, endUseB);
        }

        //Counter should only work when enabled
        Thread.Sleep(1000);

        counterA.Enabled = false;
        handled = SendCounterMessage(handler, counterA);

        startUseB = DateTimeOffset.UtcNow;
        handled &= SendCounterMessage(handler, counterB);
        endUseB = DateTimeOffset.UtcNow;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True, "All messages should be handled");
            AssertCounter(counterA, 4, startUseA, endUseA);
            AssertCounter(counterB, 4, startUseB, endUseB);
        }

        //Should still work when not visible
        Thread.Sleep(1000);
        _config.Counters_ShowNotification = false;
        counterA.Enabled = true;

        startUseA = DateTimeOffset.UtcNow;
        handled = SendCounterMessage(handler, counterA);
        endUseA = DateTimeOffset.UtcNow;

        startUseB = DateTimeOffset.UtcNow;
        handled &= SendCounterMessage(handler, counterB);
        endUseB = DateTimeOffset.UtcNow;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True, "All messages should be handled");
            AssertCounter(counterA, 5, startUseA, endUseA);
            AssertCounter(counterB, 5, startUseB, endUseB);
        }
    }

    [Test]
    public void TestCounterHandlerOutput() //todo: refactor
    {
        _config.Counters_List.Clear();
        var counterA = new CounterModel()
        {
            CooldownSeconds = 0,
            DoDisplay = true,
            Enabled = false,
            Name = "CounterA",
            Parameter = "CounterA" 
        };
        var counterB = new CounterModel()
        {
            CooldownSeconds = 0,
            DoDisplay = false,
            Enabled = true,
            Name = "CounterB",
            Parameter = "CounterB"
        };
        _config.Counters_List.AddRange([counterA, counterB]);

        _config.Counters_DisplayCooldownSeconds = 0;
        _config.Counters_DisplayDurationSeconds = 0.1f;
        _config.Counters_ShowNotification = true;

        var output = new MockOutputManagerService();
        var handler = new CounterOscMessageHandler(output, _config, _logger);

        //Should not display if disabled or dodisplay is off
        var handled = SendCounterMessage(handler, counterA)
            && SendCounterMessage(handler, counterB);

        AssertCounterOutputBasic(handled, counterA, 0, counterB, 1, output, 0);

        //Normal display test, display multiple
        Thread.Sleep(100);
        counterA.Enabled = true;
        counterB.DoDisplay = true;
        handled = SendCounterMessage(handler, counterA);
        var expectedFlag = OutputSettingsFlags.AllowTextOutput;

        AssertCounterOutputBasic(handled, counterA, 1, counterB, 1, output, 1);
        AssertCounterOutputSecondary(output, 0, counterA, true, counterB, false, expectedFlag);

        // Both should show
        Thread.Sleep(50);
        handled = SendCounterMessage(handler, counterB);

        AssertCounterOutputBasic(handled, counterA, 1, counterB, 2, output, 2);
        AssertCounterOutputSecondary(output, 1, counterA, true, counterB, true, expectedFlag);

        // Counter A should be timed out now
        Thread.Sleep(50);
        handled = SendCounterMessage(handler, counterB);

        AssertCounterOutputBasic(handled, counterA, 1, counterB, 3, output, 3);
        AssertCounterOutputSecondary(output, 2, counterA, false, counterB, true, expectedFlag);

        // Counter A should display again now
        _config.Counters_DisplayDurationSeconds = 1f;
        handled = SendCounterMessage(handler, counterB);

        AssertCounterOutputBasic(handled, counterA, 1, counterB, 4, output, 4);
        AssertCounterOutputSecondary(output, 3, counterA, true, counterB, true, expectedFlag);

        //Will not be displayed due to cooldown
        _config.Counters_DisplayCooldownSeconds = 1;
        handled = SendCounterMessage(handler, counterA)
            && SendCounterMessage(handler, counterB);

        AssertCounterOutputBasic(handled, counterA, 2, counterB, 5, output, 4);

        //Will only display the 1st due to cooldown (Not sent at same time)
        Thread.Sleep(1000);
        handled = SendCounterMessage(handler, counterA)
            && SendCounterMessage(handler, counterB);

        AssertCounterOutputBasic(handled, counterA, 3, counterB, 6, output, 5);
        AssertCounterOutputSecondary(output, 4, counterA, true, counterB, false, expectedFlag);

        //Should still show B even though we only trigger A
        _config.Counters_DisplayCooldownSeconds = 0;
        handled = SendCounterMessage(handler, counterA);

        AssertCounterOutputBasic(handled, counterA, 4, counterB, 6, output, 6);
        AssertCounterOutputSecondary(output, 5, counterA, true, counterB, true, expectedFlag);

        // Showing when disabled
        _config.Counters_ShowNotification = false;
        handled = SendCounterMessage(handler, counterA)
            && SendCounterMessage(handler, counterB);

        AssertCounterOutputBasic(handled, counterA, 5, counterB, 7, output, 6);
    }

    private void AssertCounter(CounterModel counter, int expectedCount, DateTimeOffset lastUsedStart, DateTimeOffset lastUsedEnd)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(counter.Count, Is.EqualTo(expectedCount), $"Unexpected counter count on {counter.Name}");
            Assert.That(counter.LastUsed, Is.GreaterThanOrEqualTo(lastUsedStart), $"Last use too old on {counter.Name}");
            Assert.That(counter.LastUsed, Is.LessThanOrEqualTo(lastUsedEnd), $"Last use too recent on {counter.Name}");
        }
    }

    private bool SendCounterOther(CounterOscMessageHandler handler, string parameterName, params object[] args)
    {
        var message = new OscMessage(parameterName, args);
        return handler.HandleMessage(message);
    }

    private bool SendCounterMessage(CounterOscMessageHandler handler, CounterModel counter)
    {
        var message = new OscMessage(counter.FullParameter(), true);
        return handler.HandleMessage(message);
    }

    private void AssertCounterOutputBasic(bool handled, CounterModel counterA, int counterACount, CounterModel counterB, int counterBCount, MockOutputManagerService output, int outputNotifCount)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True, "Message should be handled");
            Assert.That(counterA.Count, Is.EqualTo(counterACount));
            Assert.That(counterB.Count, Is.EqualTo(counterBCount));
            Assert.That(output.Notifications, Has.Count.EqualTo(outputNotifCount));
            Assert.That(output.Messages, Is.Empty);
        }
    }

    private void AssertCounterOutputSecondary(MockOutputManagerService output, int idx, CounterModel counterA, bool counterAContain, CounterModel counterB, bool counterBContain, OutputSettingsFlags expectedFlag)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(output.Notifications[idx].Message, counterAContain ? Does.Contain(counterA.Name) : Does.Not.Contain(counterA.Name));
            Assert.That(output.Notifications[idx].Message, counterBContain ? Does.Contain(counterB.Name) : Does.Not.Contain(counterB.Name));
            Assert.That(output.Notifications[idx].Flags, Is.EqualTo(expectedFlag), "Notification has the wrong flags");
        }
    }

    [Test]
    public void TestExternalInputMessageHandler()
    {
        var config = new ConfigModel();
        var input = new MockInputService();
        var handler = new ExternalInputMessageHandler(config, input, _logger);

        //Send inapplicable message
        var message = new OscMessage("/test", false);
        var result = handler.HandleMessage(message);
        Assert.That(result, Is.False, "Handler should not have handled the message");

        AssertExternalInput(handler, _config.Osc_Address_Input_Audio, "AudioTest",
        () => input.AudioMessages, (x) => input.AudioMessages[x]);

        AssertExternalInput(handler, _config.Osc_Address_Input_Other, "OtherTest",
        () => input.OtherMessages, (x) => input.OtherMessages[x]);

        AssertExternalInput(handler, _config.Osc_Address_Input_TextMessage, "TextMessageTest",
        () => input.TextMessages, (x) => input.TextMessages[x]);

        AssertExternalInput(handler, _config.Osc_Address_Input_TextNotification, "TextNotifTest",
        () => input.TextNotification, (x) => input.TextNotification[x].Item1);
    }

    private void AssertExternalInput<T>(ExternalInputMessageHandler handler, string address, string testText, Func<List<T>> getList, Func<int, string> getListString)
    {
        //Wrong parameter
        var message = new OscMessage(address, false);
        var result = handler.HandleMessage(message);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True, "Handler should have handled the message");
            Assert.That(getList(), Is.Empty, "Input service should not have received an audio message");
        }

        //Empty parameter
        message = new OscMessage(address, string.Empty);
        result = handler.HandleMessage(message);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True, "Handler should have handled the message");
            Assert.That(getList(), Is.Empty, "Input service should not have received a message");
        }

        //Correct Parameter
        message = new OscMessage(address, testText);
        result = handler.HandleMessage(message);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True, "Handler should have handled the message");
            Assert.That(getList(), Has.Count.EqualTo(1), "Input service should have received a message");
        }
        Assert.That(getListString(0), Is.EqualTo(testText), "Received incorrect message");
    }
}