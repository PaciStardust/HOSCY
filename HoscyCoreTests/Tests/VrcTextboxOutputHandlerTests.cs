using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Output.Handlers;
using HoscyCoreTests.Mocks.Impl;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.VrcTextboxOutputHandlerTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class VrcTextboxOutputHandlerStartupTests : TestBase<VrcTextboxOutputHandlerStartupTests>
{
    private ConfigModel _config = null!;
    private MockOscSendService _send = null!;

    private VrcTextboxOutputHandler _handler = null!;

    protected override void SetupExtra()
    {
        _config = new();
        _send = new(_config);
        _handler = new(_logger, _config, _send);
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SimpleStartStopRestartTest(_handler, false, restartNotStart, doAgain);
    }
}

public class VrcTextboxOutputHandlerFunctionTests : TestBase<VrcTextboxOutputHandlerFunctionTests>
{
    private VrcTextboxOutputHandler _handler = null!;
    private MockOscSendService _send = null!;

    private readonly ConfigModel _config = new();

    private const int TIMEOUT_WAIT_MS_2X = VrcTextboxOutputHandler.TIMEOUT_WAIT_MS * 2;

    protected override void OneTimeSetupExtra()
    {
        _send = new(_config);
        _handler = new(_logger, _config, _send);

        _handler.Start();
    }

    protected override void SetupExtra()
    {
        ClearAndWait();

        _config.VrcTextbox_Enabled = true;
        _config.VrcTextbox_Do_Indicator = true;
        _config.VrcTextbox_Do_Output = true;

        _config.VrcTextbox_Notification_IndicatorTextEnd = "!!!";
        _config.VrcTextbox_Notification_IndicatorTextStart = "???";

        _config.VrcTextbox_Timeout_UseDynamic = true;
    }

    [Test]
    public void SendProcessingIndicatorTest()
    {
        // Normal Send
        _handler.SetProcessingIndicator(true);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[0].Address, Is.EqualTo(_config.Osc_Address_Game_Typing));
            Assert.That(_send.ReceivedMessages[0].Args, Is.EqualTo([1]));
        }

        Thread.Sleep((VrcTextboxOutputHandler.INDICATOR_COOLDOWN_S - 1) * 1000);

        // Timeout still
        _handler.SetProcessingIndicator(true);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(1125);

        // No longer timed out
        _handler.SetProcessingIndicator(true);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[1].Address, Is.EqualTo(_config.Osc_Address_Game_Typing));
            Assert.That(_send.ReceivedMessages[1].Args, Is.EqualTo([1]));
        }

        Thread.Sleep(1125);

        // Clear despite timeout
        _handler.SetProcessingIndicator(false);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[2].Address, Is.EqualTo(_config.Osc_Address_Game_Typing));
            Assert.That(_send.ReceivedMessages[2].Args, Is.EqualTo([0]));
        }

        // Can not instantly send after clear
        _handler.SetProcessingIndicator(true);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(3));

        Thread.Sleep((VrcTextboxOutputHandler.INDICATOR_COOLDOWN_S - 1) * 1000);

        // Can send as clear does not affect cooldown
        _handler.SetProcessingIndicator(true);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(4));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[3].Address, Is.EqualTo(_config.Osc_Address_Game_Typing));
            Assert.That(_send.ReceivedMessages[3].Args, Is.EqualTo([1]));
        }

        // Clear also does typing indicator
        _handler.Clear();
        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_WAIT_MS * 2);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(6)); //Increase by 2 because clear
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[4].Address, Is.EqualTo(_config.Osc_Address_Game_Typing));
            Assert.That(_send.ReceivedMessages[4].Args, Is.EqualTo([0]));
        }

        Thread.Sleep(VrcTextboxOutputHandler.INDICATOR_COOLDOWN_S * 1000 + 125);

        _config.VrcTextbox_Do_Indicator = false;

        // Cant send because indicator disabled
        _handler.SetProcessingIndicator(true);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(6)); 

        _config.VrcTextbox_Do_Indicator = true;

        // Send again
        _handler.SetProcessingIndicator(true);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(7));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[6].Address, Is.EqualTo(_config.Osc_Address_Game_Typing));
            Assert.That(_send.ReceivedMessages[6].Args, Is.EqualTo([1]));
        }

        _config.VrcTextbox_Do_Indicator = false;

        // Can clear even tho indicator is off
        _handler.SetProcessingIndicator(false);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(8));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[7].Address, Is.EqualTo(_config.Osc_Address_Game_Typing));
            Assert.That(_send.ReceivedMessages[7].Args, Is.EqualTo([0]));
        }
    }

    [TestCase(true), TestCase(false)]
    public void SendSimpleNotificationTest(bool doSound)
    {
        _config.VrcTextbox_Sound_OnNotification = doSound;

        _handler.HandleNotification("123abc", OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[0].Address, Is.EqualTo(_config.Osc_Address_Game_Textbox));
            object[] expectedArgs = [true, _config.VrcTextbox_Sound_OnNotification];
            Assert.That(_send.ReceivedMessages[0].Args.Skip(1).ToArray(), Is.EqualTo(expectedArgs));
            Assert.That(_send.ReceivedMessages[0].Args[0], Does.Contain("123abc"));
            Assert.That(_send.ReceivedMessages[0].Args[0], Does.StartWith(_config.VrcTextbox_Notification_IndicatorTextStart));
            Assert.That(_send.ReceivedMessages[0].Args[0], Does.EndWith(_config.VrcTextbox_Notification_IndicatorTextEnd));
            var composite = _config.VrcTextbox_Notification_IndicatorTextStart + "123abc" + _config.VrcTextbox_Notification_IndicatorTextEnd;
            Assert.That(_send.ReceivedMessages[0].Args[0], Is.EqualTo(composite));
        }
    }

    [TestCase(true), TestCase(false)]
    public void SendSimpleMessageTest(bool doSound)
    {
        _config.VrcTextbox_Sound_OnMessage = doSound;

        _handler.HandleMessage("123abc");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[0].Address, Is.EqualTo(_config.Osc_Address_Game_Textbox));
            object[] expectedArgs = [true, _config.VrcTextbox_Sound_OnMessage];
            Assert.That(_send.ReceivedMessages[0].Args.Skip(1).ToArray(), Is.EqualTo(expectedArgs));
            Assert.That(_send.ReceivedMessages[0].Args[0], Is.EqualTo("123abc"));
        }
    }

    [TestCase(true), TestCase(false)]
    public void DoOutputTest(bool doOutput)
    {
        _config.VrcTextbox_Do_Output = doOutput;

        // Sending a notification
        _handler.HandleNotification("123abc", OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(doOutput ? 1 : 0));
        if (doOutput)
        {
            Assert.That(_send.ReceivedMessages[0].Address, Is.EqualTo(_config.Osc_Address_Game_Textbox));
        }

        ClearAndWait();

        // Sending a message
        _handler.HandleMessage("123abc");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(doOutput ? 1 : 0));
        if (doOutput)
        {
            Assert.That(_send.ReceivedMessages[0].Address, Is.EqualTo(_config.Osc_Address_Game_Textbox));
        }
        _send.ReceivedMessages.Clear();

        // Send processing indicator (Indep from processing indicator)
        _handler.SetProcessingIndicator(true);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));
        Assert.That(_send.ReceivedMessages[0].Address, Is.EqualTo(_config.Osc_Address_Game_Typing));
    }

    [Test]
    public void DoesInfoWorkTest()
    {
        var info = new VrcTextboxOutputHandlerStartInfo(_config);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.ModuleType, Is.EqualTo(typeof(VrcTextboxOutputHandler)));
            Assert.That(info.ShouldBeEnabled(), Is.True);
        }

        _config.VrcTextbox_Enabled = false;

        Assert.That(info.ShouldBeEnabled(), Is.False);
    }

    [Test]
    public void DoesTranslateInfoWorkTest()
    {
        _config.VrcTextbox_Output_ShowTranslation = false;
        Assert.That(_handler.GetTranslationOutputMode(), Is.EqualTo(OutputTranslationFormat.Untranslated));

        _config.VrcTextbox_Output_ShowTranslation = true;
        Assert.That(_handler.GetTranslationOutputMode(), Is.EqualTo(OutputTranslationFormat.Both));
    }

    [TestCase(OutputNotificationPriority.Minimal, true)]
    [TestCase(OutputNotificationPriority.Medium, true)]
    [TestCase(OutputNotificationPriority.Critical, true)]
    [TestCase(OutputNotificationPriority.Critical, false)]
    public void NotificationPriorityTest(OutputNotificationPriority otherPrio, bool enabled)
    {
        _config.VrcTextbox_Notification_UsePrioritySystem = enabled;

        const OutputNotificationPriority DEFAULT_PRIO = OutputNotificationPriority.Medium; 
        bool shouldOverride = otherPrio >= DEFAULT_PRIO && enabled;

        // Override by shortening timeout
        _handler.HandleNotification(string.Empty.PadLeft(50, 'a'), DEFAULT_PRIO);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        _handler.HandleNotification("456def", otherPrio);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(shouldOverride ? 2 : 1));

        ClearAndWait();
        _config.VrcTextbox_Timeout_UseDynamic = false;
        _config.VrcTextbox_Timeout_StaticMs = 1000;

        // Override before it is sent
        _handler.HandleNotification("123abc", DEFAULT_PRIO);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        _handler.HandleNotification("456def", DEFAULT_PRIO);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        _handler.HandleNotification("789ghi", otherPrio);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));
        Assert.That(_send.ReceivedMessages[1].Args[0], Does.Contain(shouldOverride || !enabled ? "789ghi" : "456def"));
    }

    [TestCase(true), TestCase(false)]
    public void IsNotificationSkippedByMessageTest(bool enabled)
    {
        _config.VrcTextbox_Notification_SkipWhenMessageAvailable = enabled;

        _handler.HandleNotification("123abc", OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);

        _handler.HandleMessage("456def");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(enabled ? 2 : 1));
    }

    [TestCase(true), TestCase(false)]
    public void AutomaticMessageClearTest(bool enabled)
    {
        _config.VrcTextbox_Timeout_UseDynamic = false;
        _config.VrcTextbox_Timeout_StaticMs = VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS;

        _config.VrcTextbox_Timeout_AutomaticallyClearMessage = enabled;

        _handler.HandleMessage("123abc");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(enabled ? 2 : 1));
    }

    [TestCase(true), TestCase(false)]
    public void AutomaticNotificationClearTest(bool enabled)
    {
        _config.VrcTextbox_Timeout_UseDynamic = false;
        _config.VrcTextbox_Timeout_StaticMs = VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS;

        _config.VrcTextbox_Timeout_AutomaticallyClearNotification = enabled;

        _handler.HandleNotification("123abc", OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(enabled ? 2 : 1));
    }

    [Test]
    public void ClearTest()
    {
        // Send message
        _handler.HandleMessage("123abc");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        // Clear does not happen yet because timeout
        _handler.Clear();
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        // Clear hits
        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));
        Assert.That(_send.ReceivedMessages[1].Args[0], Is.Empty);

        // Message does not send yet due to timeout
        _handler.HandleMessage("123abc");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));

        // Message hits
        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(3));
    }

    [Test]
    public void DynamicTimeoutTest()
    {
        _config.VrcTextbox_Timeout_DynamicMinimumMs = VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + 250;
        _config.VrcTextbox_Timeout_DynamicPer20CharactersDisplayedMs = _config.VrcTextbox_Timeout_DynamicMinimumMs / 2 + 250;

        // Timeout should be minimum
        _handler.HandleMessage(string.Empty.PadRight(19, 'a'));
        _handler.HandleMessage(string.Empty.PadRight(19, 'a'));
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(250);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));

        ClearAndWait();
        
        // Increased timeout because more characters
        _handler.HandleMessage(string.Empty.PadRight(21, 'a'));
        _handler.HandleMessage(string.Empty.PadRight(19, 'a'));
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(_config.VrcTextbox_Timeout_DynamicMinimumMs);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(500);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));

        ClearAndWait();

        // Increased minimum timeout
        _config.VrcTextbox_Timeout_DynamicMinimumMs = VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS * 2;

        _handler.HandleMessage("123abc");
        _handler.HandleMessage("123abc");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS / 2);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS / 2);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));
    }

    [Test]
    public void StaticTimeoutTest()
    {
        _config.VrcTextbox_Timeout_UseDynamic = false;
        _config.VrcTextbox_Timeout_StaticMs = 2000;

        _handler.HandleMessage("1");
        _handler.HandleMessage("2");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(_config.VrcTextbox_Timeout_StaticMs / 4 * 3);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(_config.VrcTextbox_Timeout_StaticMs / 4);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));

        ClearAndWait();

        _config.VrcTextbox_Timeout_StaticMs = 1000;

        _handler.HandleMessage("3");
        _handler.HandleMessage("4");
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(_config.VrcTextbox_Timeout_StaticMs / 4 * 3);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));

        Thread.Sleep(_config.VrcTextbox_Timeout_StaticMs / 4);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));
    }

    [Test]
    public void NotificationLengthLimitTest()
    {
        _config.VrcTextbox_Output_MaxDisplayedCharacters = 20;
        _config.VrcTextbox_Notification_IndicatorTextStart = "3ch";
        _config.VrcTextbox_Notification_IndicatorTextEnd = "3ch";

        _config.VrcTextbox_Notification_UsePrioritySystem = true;

        // Message total length = 11/20 => fits
        var message = "12345678901";
        _handler.HandleNotification(message, OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[0].Args[0], Does.Contain(message));
            Assert.That(_send.ReceivedMessages[0].Args[0], Does.StartWith(_config.VrcTextbox_Notification_IndicatorTextStart));
            Assert.That(_send.ReceivedMessages[0].Args[0], Does.EndWith(_config.VrcTextbox_Notification_IndicatorTextEnd));
        }

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);

        // Message total length = 20/20 => fits
        message = "12345678901234567890";
        _handler.HandleNotification(message, OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[1].Args[0], Does.Contain(message));
            Assert.That(_send.ReceivedMessages[1].Args[0], Does.StartWith(_config.VrcTextbox_Notification_IndicatorTextStart));
            Assert.That(_send.ReceivedMessages[1].Args[0], Does.EndWith(_config.VrcTextbox_Notification_IndicatorTextEnd));
        }

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);

        // Message total length = 24/20 => crops
        message = "123456789012345678901234";
        _handler.HandleNotification(message, OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[2].Args[0], Does.Not.Contain(message));
            Assert.That(_send.ReceivedMessages[2].Args[0], Does.Contain(message[..19]));
            Assert.That(_send.ReceivedMessages[2].Args[0], Does.Not.Contain(message[..20]));
            Assert.That(_send.ReceivedMessages[2].Args[0], Does.StartWith(_config.VrcTextbox_Notification_IndicatorTextStart));
            Assert.That(_send.ReceivedMessages[2].Args[0], Does.EndWith(_config.VrcTextbox_Notification_IndicatorTextEnd));
        }

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);
        _config.VrcTextbox_Notification_IndicatorTextStart = ".";
        _config.VrcTextbox_Notification_IndicatorTextEnd = ".";

        // Message total length = 20/20 => fits
        message = "12345678901234567890";
        _handler.HandleNotification(message, OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(4));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[3].Args[0], Does.Contain(message));
            Assert.That(_send.ReceivedMessages[3].Args[0], Does.StartWith(_config.VrcTextbox_Notification_IndicatorTextStart));
            Assert.That(_send.ReceivedMessages[3].Args[0], Does.EndWith(_config.VrcTextbox_Notification_IndicatorTextEnd));
        }

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);

        _config.VrcTextbox_Output_MaxDisplayedCharacters = 40;

        // Message total length = 40/40 => fits
        message += message;
        _handler.HandleNotification(message, OutputNotificationPriority.Medium);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(5));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_send.ReceivedMessages[4].Args[0], Does.Contain(message));
            Assert.That(_send.ReceivedMessages[4].Args[0], Does.StartWith(_config.VrcTextbox_Notification_IndicatorTextStart));
            Assert.That(_send.ReceivedMessages[4].Args[0], Does.EndWith(_config.VrcTextbox_Notification_IndicatorTextEnd));
        }
    }

    [Test]
    public void MessageSplittingTest()
    {
        _config.VrcTextbox_Timeout_UseDynamic = false;
        _config.VrcTextbox_Timeout_StaticMs = VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS;

        _config.VrcTextbox_Output_MaxDisplayedCharacters = 20;

        // Message fits
        var message = "1234567890";
        _handler.HandleMessage(message);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(1));
        Assert.That(_send.ReceivedMessages[0].Args[0], Is.EqualTo(message));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);

        // Message barely fits
        message = "12345678901234567890";
        _handler.HandleMessage(message);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(2));
        Assert.That(_send.ReceivedMessages[1].Args[0], Is.EqualTo(message));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);

        // Message should be split by last space
        message = "123456 8901234567 89012";
        _handler.HandleMessage(message);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(3));
        Assert.That(_send.ReceivedMessages[2].Args[0], Is.EqualTo("123456 8901234567 ..."));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(4));
        Assert.That(_send.ReceivedMessages[3].Args[0], Is.EqualTo("... 89012"));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);

        // Message should be split by last space again but ignore all whitespace
        message = "123456 8901234567       \r\n\t        89012";
        _handler.HandleMessage(message);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(5));
        Assert.That(_send.ReceivedMessages[4].Args[0], Is.EqualTo("123456 8901234567 ..."));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(6));
        Assert.That(_send.ReceivedMessages[5].Args[0], Is.EqualTo("... 89012"));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);

        // Message can not be split properly
        message = "123456789012345678901234567890 123456";
        _handler.HandleMessage(message);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(7));
        Assert.That(_send.ReceivedMessages[6].Args[0], Is.EqualTo("1234567890123456789- ..."));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(8));
        Assert.That(_send.ReceivedMessages[7].Args[0], Is.EqualTo("... 123456"));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);

        // Short to long word
        message = "12345 7890123456789012345";
        _handler.HandleMessage(message);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(9));
        Assert.That(_send.ReceivedMessages[8].Args[0], Is.EqualTo("12345 ..."));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS + TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(10));
        Assert.That(_send.ReceivedMessages[9].Args[0], Is.EqualTo("... 7890123456789012345"));

        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS);
        _config.VrcTextbox_Output_MaxDisplayedCharacters = 40;

        // Limit change
        message = "123456789012345678901234567890 123456";
        _handler.HandleMessage(message);
        Thread.Sleep(TIMEOUT_WAIT_MS_2X);
        Assert.That(_send.ReceivedMessages, Has.Count.EqualTo(11));
        Assert.That(_send.ReceivedMessages[10].Args[0], Is.EqualTo(message));
    }

    protected override void OneTimeTearDownExtra()
    {
        _handler.Stop();
    }

    private void ClearAndWait()
    {
        _handler.Clear();
        Thread.Sleep(VrcTextboxOutputHandler.TIMEOUT_MINIMUM_MS * 2 + TIMEOUT_WAIT_MS_2X);
        _send.Clear();
    }
}