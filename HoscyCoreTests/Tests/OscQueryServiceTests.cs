using HoscyCore.Services.Osc.Query;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.OscQueryServiceTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class OscQueryServiceStartupTests : TestBase<OscQueryServiceStartupTests>
{
    private MockBackToFrontNotifyService _notify = null!;
    private MockOscListenService _listen = null!;
    private OscQueryHostRegistry _registry = null!;

    private OscQueryService _query = null!;

    protected override void SetupExtra()
    {
        _notify = new();
        _listen = new();
        _registry = new(_logger);

        _query = new(_logger, _notify, _listen, _registry);
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SimpleStartStopRestartTest(_query, false, restartNotStart, doAgain);
    }
}

public class OscQueryServiceFunctionTests : TestBase<OscQueryServiceFunctionTests>
{
    private OscQueryService _query = null!;
    private readonly MockBackToFrontNotifyService _notify = new();
    private readonly MockOscListenService _listen = new();
    private OscQueryHostRegistry _registry = null!;

    protected override void OneTimeSetupExtra()
    {
        _listen.Start();
        _registry = new(_logger);

        _query = new(_logger, _notify, _listen, _registry);
        _query.Start();
    }

    [Test]
    public void QueryTest()
    {
        //We only wait and hope self descovery works and that the registry has a "self" assigned
        Thread.Sleep(5000);
        
        var self = _registry.GetServiceAddressByName("self");
        var hoscy = _registry.GetServiceAddressByName("hoscy");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(self, Is.Not.Null);
            Assert.That(hoscy, Is.Not.Null);
        }
    }

    protected override void OneTimeTearDownExtra()
    {
        _query.Stop();
    }
}