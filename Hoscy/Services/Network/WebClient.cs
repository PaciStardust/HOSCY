using System.Net.Http;
using System.Threading.Tasks;
using Hoscy.Services.DependencyCore;
using Serilog;

namespace Hoscy.Services.Network;

[LoadIntoDiContainer(typeof(IWebClient), Lifetime.Singleton)]
public class WebClient(ILogger logger) : StartStopServiceBase, IWebClient
{
    private readonly ILogger _logger = logger.ForContext<WebClient>();
    private readonly HttpClient? _client = null;

    public Task<bool> DownloadAsync(string sourceUrl, string fileLocation, int timeoutMs = 5000)
    {
        throw new System.NotImplementedException();
    }

    public override bool IsRunning()
    {
        throw new System.NotImplementedException();
    }

    public override void Restart()
    {
        throw new System.NotImplementedException();
    }

    public Task<string?> SendAsync(HttpRequestMessage requestMessage, int timeoutMs = 5000)
    {
        throw new System.NotImplementedException();
    }

    public override void Stop()
    {
        throw new System.NotImplementedException();
    }

    protected override void StartInternal()
    {
        throw new System.NotImplementedException();
    }
}