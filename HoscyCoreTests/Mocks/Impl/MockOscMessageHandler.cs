using HoscyCore.Services.Osc.MessageHandling;
using LucHeart.CoreOSC;

namespace HoscyCoreTests.Mocks.Impl;

public abstract class MockOscMessageHandler : IOscMessageHandler
{
    public readonly List<OscMessage> ReceivedMessages = [];
    public bool ReturnValue { get; set; } = false;

    public bool HandleMessage(OscMessage message)
    {
        ReceivedMessages.Add(message);
        return ReturnValue;
    }
}

public class MockOscMessageHandlerA : MockOscMessageHandler { }
public class MockOscMessageHandlerB : MockOscMessageHandler { }