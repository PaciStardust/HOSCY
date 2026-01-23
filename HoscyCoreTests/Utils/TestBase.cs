using System.Diagnostics;
using Serilog;

namespace HoscyCoreTests.Utils;

[TestFixture]
public abstract class TestBase<T>
{
    protected ILogger _logger = TestUtils.GetLogger<T>();
    protected string _tempFolder = string.Empty;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _logger.Information("OneTimeSetup - {TestFixture}", TestContext.CurrentContext.Test.ClassName);
        _tempFolder = TestUtils.GenerateTempFolder();
        var startupTimer = Stopwatch.StartNew();
        OneTimeSetupExtra();
        startupTimer.Stop();
        TestContext.Out.WriteLine($"Startup time: {startupTimer.ElapsedMilliseconds} ms");
        _logger.Information("OneTimeSetup Done - {TestFixture} - {time}ms", TestContext.CurrentContext.Test.ClassName, startupTimer.ElapsedMilliseconds);
    }
    protected virtual void OneTimeSetupExtra() {}

    [SetUp]
    public void Setup()
    {
        _logger.Information("Setup - {Test}", TestContext.CurrentContext.Test.Name);
        SetupExtra();
        _logger.Information("Setup Done - {Test}", TestContext.CurrentContext.Test.Name);
    }

    protected virtual void SetupExtra() {}

    [TearDown]
    public void TearDown()
    {
        _logger.Information("TearDown - {Test}", TestContext.CurrentContext.Test.Name);
        TearDownExtra();
        _logger.Information("TearDown Done - {Test}", TestContext.CurrentContext.Test.Name);
    }
    protected virtual void TearDownExtra() {}

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _logger.Information("OneTimeTearDown - {TestFixture}", TestContext.CurrentContext.Test.ClassName);
        var teardownTimer = Stopwatch.StartNew();
        OneTimeTearDownExtra();
        teardownTimer.Stop();
        TestContext.Out.WriteLine($"Shutdown time: {teardownTimer.ElapsedMilliseconds} ms");
        _logger.Information("OneTimeTearDown Done - {TestFixture} - {time}ms", TestContext.CurrentContext.Test.ClassName, teardownTimer.ElapsedMilliseconds);
    }
    protected virtual void OneTimeTearDownExtra() {}
}