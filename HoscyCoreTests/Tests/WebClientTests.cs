using HoscyCore.Services.Network;
using HoscyCore.Utility;
using HoscyCoreTests.Utils;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HoscyCoreTests.Tests.WebClientTests;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class WebClientStartupTests : TestBase<WebClientStartupTests>
{
    private WebClient _client = null!;

    protected override void SetupExtra()
    {
        _client = new(_logger);
    }

    [TestCase(false, false), TestCase(true, false), TestCase(false, true)]
    public void StartStopRestartTest(bool restartNotStart, bool doAgain)
    {
        SimpleStartStopRestartTest(_client, false, restartNotStart, doAgain);
    }
}

public class WebClientFunctionTests : TestBase<WebClientFunctionTests>
{
    private WebClient _client = null!;

    protected override void OneTimeSetupExtra()
    {
        var client = new WebClient(_logger);
        client.Start().AssertOk();
        _client = client;

        AssertServiceProcessing(_client);
    }

    [Test]
    public async Task TestDownloadAsync()
    {
        var path = Path.Join(_tempFolder, "dltest.html");
        (await _client.DownloadAsync("https://paci.dev/", path)).AssertOk();
        
        var fileInfo = new FileInfo(path);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fileInfo.Exists, "File does not exist");
            Assert.That(fileInfo.Length, Is.GreaterThan(0), "File should not be empty");
        };
    }

    [Test]
    public async Task TestPostAsync()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://paci.dev/");
        var result = await _client.SendAsync(requestMessage);

        result.AssertOk();
        Assert.That(result.Value, Is.Not.Empty, "Result was empty");
    }

    protected override void OneTimeTearDownExtra()
    {
        _client.Stop().AssertOk();
        AssertServiceStopped(_client);
    }
}