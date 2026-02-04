using HoscyCore.Services.Osc.MessageHandling;
using LucHeart.CoreOSC;

namespace HoscyCoreTests.Mocks;

public class MockOscMessageHandlingService : MockStartStopServiceBase, IOscMessageHandlingService
{
    public readonly List<OscMessage> ReceivedMessages = [];
    public bool DoesHandle { get; set; } = true;

    public bool HandleMessage(OscMessage message)
    {
        ReceivedMessages.Add(message);
        return DoesHandle;
    }
}