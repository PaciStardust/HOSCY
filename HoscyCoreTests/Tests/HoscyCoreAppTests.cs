using HoscyCore;
using HoscyCore.Configuration.Modern;
using HoscyCoreTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HoscyCoreTests.Tests;

public class HoscyCoreAppTests : TestBaseForService<HoscyCoreAppTests>
{
    private HoscyCoreApp _coreApp = null!;
    private readonly ConfigModel _config = new()
    {
        Afk_StartText = "Hi I am a Config"
    };

    protected override void OneTimeSetupExtra()
    {
        var coreApp = new HoscyCoreApp(_logger);
        var config = new HoscyCoreAppStartParameters()
        {
            AdditionalContainerInserts = x =>
            {
                x.AddSingleton<DiTestService2>();
            },
            CreateLoggerFromConfiguration = false,
            DisableConsoleLog = true,
            PreloadedConfig = _config,
            ShouldOpenConsoleIfRequested = false
        };
        coreApp.Start(config);
        _coreApp = coreApp;
    }

    [Test]
    public void GetService()
    {
        Assert.That(_coreApp, Is.Not.Null, message: "Core App is null!");
        var container = _coreApp.GetContainer();

        Assert.That(container, Is.Not.Null, "Container is null!");
        var newLogger = container.GetService<ILogger>();
        Assert.That(newLogger, Is.Not.Null, "Logger from container is null");
        var newConfig = container.GetRequiredService<ConfigModel>();
        Assert.That(newConfig.Afk_StartText, Is.EqualTo(_config.Afk_StartText), "Did not retrieve init config");

        var shouldPass = container.GetService<IDiTestService>();
        Assert.That(shouldPass, Is.Not.Null, "Di Test shouldve worked");
        var shouldFail = container.GetService<DiTestService>();
        Assert.That(shouldFail, Is.Null, "Di Test shouldve not worked");
        var shouldPass2 = container.GetService<DiTestService2>();
        Assert.That(shouldPass2, Is.Not.Null, "Di Test shouldve worked");
    }

    protected override void OneTimeTearDownExtra()
    {
        _coreApp.Stop();
    }
}