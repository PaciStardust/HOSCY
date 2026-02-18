using HoscyCore.Services.Osc.MessageHandling;
using HoscyCoreTests.Mocks.Base;
using LucHeart.CoreOSC;

namespace HoscyCoreTests.Mocks.Impl;

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