using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCoreTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.DiContainerTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class DiContainerStartupTests : TestBase<DiContainerStartupTests>
{
    private ConfigModel _config = null!;

    protected override void SetupExtra()
    {
        _config = new();
    }

    [Test]
    public void StartStopTest()
    {
        List<string> progress = [];

        var container = DiContainer.CreateWithAssembly(_logger, _config, null);
        
        container.StartServices(x => progress.Add(x));
        Assert.That(progress, Is.Not.Empty);

        container.StopServices();
    }

    [Test]
    public void EmptyTest()
    {
        List<string> progress = [];

        var container = DiContainer.Empty();
        
        container.StartServices(x => progress.Add(x));
        Assert.That(progress, Is.Not.Empty);

        container.StopServices();
    }
}

public class DiContainerFunctionTests : TestBase<DiContainerFunctionTests>
{
    private DiContainer _container = null!;
    private readonly ConfigModel _config = new()
    {
        Afk_StartText = "Hi I am a Config"
    };

    protected override void OneTimeSetupExtra()
    {
        _container = DiContainer.CreateWithAssembly(_logger, _config, x =>
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