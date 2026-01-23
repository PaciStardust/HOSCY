using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCoreTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HoscyCoreTests.Tests;

public class DiContainerTests : TestBaseForService<DiContainerTests>
{
    private DiContainer _container = null!;
    private readonly ConfigModel _config = new()
    {
        Afk_StartText = "Hi I am a Config"
    };

    protected override void OneTimeSetupExtra()
    {
        _container = DiContainer.LoadFromAssembly(_logger, _config, x =>
        {
            x.AddSingleton<DiTestService2>();
        });
        _container.StartServices(null);
    }

    [Test]
    public void GetService()
    {
        Assert.That(_container, Is.Not.Null, "Container is null!");
        var newLogger = _container.GetService<ILogger>();
        Assert.That(newLogger, Is.Not.Null, "Logger from container is null");
        var newConfig = _container.GetRequiredService<ConfigModel>();
        Assert.That(newConfig.Afk_StartText, Is.EqualTo(_config.Afk_StartText), "Did not retrieve init config");

        var shouldPass = _container.GetService<IDiTestService>();
        Assert.That(shouldPass, Is.Not.Null, "Di Test shouldve worked");
        var shouldFail = _container.GetService<DiTestService>();
        Assert.That(shouldFail, Is.Null, "Di Test shouldve not worked");
        var shouldPass2 = _container.GetService<DiTestService2>();
        Assert.That(shouldPass2, Is.Not.Null, "Di Test shouldve worked");
    }

    protected override void OneTimeTearDownExtra()
    {
        _container.StopServices();
    }
}