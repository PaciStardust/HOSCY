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
        OneTimeSetupExtra();
        _logger.Information("OneTimeSetup Done - {TestFixture}", TestContext.CurrentContext.Test.ClassName);
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
        OneTimeTearDownExtra();
        _logger.Information("OneTimeTearDown Done - {TestFixture}", TestContext.CurrentContext.Test.ClassName);
    }
    protected virtual void OneTimeTearDownExtra() {}
}