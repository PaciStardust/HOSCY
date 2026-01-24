using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Network;

namespace HoscyCoreTests.Mocks;

public class MockWebClient : IWebClient
{
    public string DownloadContents { get; set; } = string.Empty;
    public string SendResult { get; set; } = string.Empty;
    public readonly List<HttpRequestMessage> Requests = [];
    public int ArtificialDelayMs { get; set; } = 0;

    public bool Running { get; private set; } = false;

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

    public ServiceStatus GetCurrentStatus()
    {
        return Running ? (
            GetFaultIfExists() is null
            ? ServiceStatus.Processing
            : ServiceStatus.Faulted
        )
        : ServiceStatus.Stopped;
    }

    public Exception? GetFaultIfExists()
    {
        return null;
    }

    public void Restart()
    {
        Stop();
        Start();
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

    public void Start()
    {
        Requests.Clear();
        Running = true;
    }

    public void Stop()
    {
        Running = false;
        Requests.Clear();
    }
}