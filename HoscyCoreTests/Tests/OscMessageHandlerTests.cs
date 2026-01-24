using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Misc;
using HoscyCore.Services.Osc.MessageHandling.Handlers;
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
}