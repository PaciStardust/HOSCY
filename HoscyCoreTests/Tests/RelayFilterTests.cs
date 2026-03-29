using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Osc.Relay;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.RelayFilterTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class RelayFilterFunctionTests : TestBase<RelayFilterFunctionTests>
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