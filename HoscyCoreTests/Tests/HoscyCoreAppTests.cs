using HoscyCore;
using HoscyCore.Configuration.Modern;
using HoscyCoreTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.HoscyCoreAppTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class HoscyCoreAppStartupTests : TestBase<HoscyCoreAppStartupTests>
{
    private ConfigModel _config = null!;

    protected override void SetupExtra()
    {
        Thread.Sleep(1000);
        _config = new();
    }

    [Test]
    public void StartStopTest()
    {
        var coreApp = new HoscyCoreApp(_logger);
        var config = HoscyCoreAppTestUtil.CreateSimpleStartupParameters(_config);

        coreApp.Start(config);
        coreApp.Stop();
    }

    [Test]
    public void CreateNewLogStartStopTest()
    {
        var coreApp = new HoscyCoreApp(_logger);
        var config = HoscyCoreAppTestUtil.CreateSimpleStartupParameters(_config);
        config.PreloadedConfig = null;
        config.CreateNewConfigIfMissing = true;
        config.CreateLoggerFromConfiguration = true;

        coreApp.Start(config);
        coreApp.Stop();
    }
}

public class HoscyCoreAppFunctionTests : TestBase<HoscyCoreAppFunctionTests>
{
    private HoscyCoreApp _coreApp = null!;
    private readonly ConfigModel _config = new()
    {
        Afk_StartText = "Hi I am a Config"
    };

    protected override void OneTimeSetupExtra()
    {
        var coreApp = new HoscyCoreApp(_logger);
        var config = HoscyCoreAppTestUtil.CreateSimpleStartupParameters(_config);
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

internal static class HoscyCoreAppTestUtil {
    internal static HoscyCoreAppStartParameters CreateSimpleStartupParameters(ConfigModel configModel)
    {
        var config = new HoscyCoreAppStartParameters()
        {
            AdditionalContainerInserts = x =>
            {
                x.AddSingleton<DiTestService2>();
            },
            CreateLoggerFromConfiguration = false,
            DisableConsoleLog = true,
            PreloadedConfig = configModel,
            ShouldOpenConsoleIfRequested = false
        };
        return config;
    }
}