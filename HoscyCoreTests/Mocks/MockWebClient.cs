using HoscyCore.Services.Dependency;
using HoscyCore.Services.Network;

namespace HoscyCoreTests.Mocks;

public class MockWebClient : MockStartStopServiceBase, IWebClient
{
    public string DownloadContents { get; set; } = string.Empty;
    public string SendResult { get; set; } = string.Empty;
    public readonly List<HttpRequestMessage> Requests = [];
    public int ArtificialDelayMs { get; set; } = 0;

    public async Task DownloadAsync(string _, string fileLocation, int timeoutMs = 5000)
    {
        if (ArtificialDelayMs > timeoutMs)
        {
            await Task.Delay(timeoutMs);
            throw new TimeoutException("MockWebClient: Download timed out");
        }
        await Task.Delay(ArtificialDelayMs);
        File.WriteAllText(fileLocation, DownloadContents);
    }

    public async Task<string> SendAsync(HttpRequestMessage requestMessage, int timeoutMs = 5000)
    {
        Requests.Add(requestMessage);
        if (ArtificialDelayMs > timeoutMs)
        {
            await Task.Delay(timeoutMs);
            throw new TimeoutException("MockWebClient: Send timed out");
        }
        await Task.Delay(ArtificialDelayMs);
        return SendResult;
    }

    public override void Start()
    {
        Requests.Clear();
        base.Start();
    }

    public override void Stop()
    {
        base.Stop();
        Requests.Clear();
    }
}