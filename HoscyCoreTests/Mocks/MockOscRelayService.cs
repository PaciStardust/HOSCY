using HoscyCore.Services.Osc.Relay;
using LucHeart.CoreOSC;

namespace HoscyCoreTests.Mocks;

public class MockOscRelayService : MockStartStopServiceBase, IOscRelayService
{
    public readonly List<string> InvalidFilters = [];
    public readonly List<OscMessage> ReceivedMessages = [];

    public string[] GetInvalidFilterNames()
        => InvalidFilters.ToArray();

    public void HandleRelay(OscMessage message)
    {
        ReceivedMessages.Add(message);
    }

    public void Clear()
    {
        InvalidFilters.Clear();
        ReceivedMessages.Clear();
    }
}