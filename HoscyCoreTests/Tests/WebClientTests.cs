using HoscyCore.Services.Network;
using HoscyCoreTests.Utils;

namespace HoscyCoreTests.Tests;

public class WebClientTests : TestBaseForService<WebClientTests>
{
    private WebClient _client = null!;

    protected override void OneTimeSetupExtra()
    {
        _client = new WebClient(_logger);
        _client.Start();

        AssertServiceStarted(_client);
    }

    [Test]
    public async Task TestDownloadAsync()
    {
        var path = Path.Join(_tempFolder, "dltest.html");
        await _client.DownloadAsync("https://paci.dev/", path);
        
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

        Assert.That(result, Is.Not.Empty, "Result was empty");
    }

    protected override void OneTimeTearDownExtra()
    {
        _client.Stop();
        AssertServiceStopped(_client);
    }
}