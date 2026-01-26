using HoscyCore.Services.Osc.Query;
using HoscyCoreTests.Mocks;
using HoscyCoreTests.Utils;
using NUnit.Framework.Interfaces;

namespace HoscyCoreTests.Tests;

public class OscQueryServiceTests : TestBaseForService<OscQueryServiceTests>
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
        AssertServiceProcessing(_query);
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
        AssertServiceStopped(_query);
    }
}