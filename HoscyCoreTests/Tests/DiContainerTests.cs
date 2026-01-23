using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCoreTests.Utils;
using Serilog;

namespace HoscyCoreTests.Tests;

public class DiContainerTests : TestBase<DiContainerTests>
{
    private DiContainer? _container;
    private readonly ConfigModel _config = new()
    {
        Afk_StartText = "Hi I am a Config"
    };

    [Test, Order(int.MinValue)]
    public void Start()
    {
        _container = DiContainer.LoadFromAssembly(_logger, _config);
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
    }

    [Test, Order(int.MaxValue)]
    public void Stop()
    {
        Assert.That(_container, Is.Not.Null, "Container is null!");
        _container.StopServices();
    }
}