using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Osc.Relay;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class RelayFilterTests : TestBase<RelayFilterTests>
{
    [TestCase(true)]
    [TestCase(false)]
    public void FilterTest(bool blacklist)
    {
        var filter = new OscRelayFilterModel()
        {
            BlacklistMode = blacklist,
            Filters = ["/a", "/b", "cat"]
        };
        var readonlyFilter = new OscReadonlyRelayFilter(filter);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(readonlyFilter.Matches("/aTest"), blacklist ? Is.False : Is.True);
            Assert.That(readonlyFilter.Matches("/bTest"), blacklist ? Is.False : Is.True);
            Assert.That(readonlyFilter.Matches("/cTest"), blacklist ? Is.True : Is.False);
            Assert.That(readonlyFilter.Matches("/catss"), blacklist ? Is.True : Is.False);
        }
    }
}