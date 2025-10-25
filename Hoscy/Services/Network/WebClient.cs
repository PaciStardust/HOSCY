using System;
using System.Net.Http;
using System.Threading.Tasks;
using Hoscy.Services.DependencyCore;
using Serilog;

namespace Hoscy.Services.Network;

[LoadIntoDiContainer(typeof(IWebClient), Lifetime.Singleton)]
public class WebClient(ILogger logger) : StartStopServiceBase, IWebClient
{
    private readonly ILogger _logger = logger.ForContext<WebClient>();
    private HttpClient? _client = null;

    #region Start / Stop
    protected override void StartInternal()
    {
        _logger.Information("Setting up HttpClient");
        var client = new HttpClient(new SocketsHttpHandler()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            UseProxy = false,
        });

        //Below is required for Github Access
        client.DefaultRequestHeaders.UserAgent.Add(new("User-Agent", "request"));
        _logger.Information("HttpClient set up");
    }

    public override void Stop()
    {
        _logger.Information("Disposing of HttpClient");
        _client?.Dispose();
        _client = null;
        _logger.Information("Disposed of HttpClient");
    }

    public override bool IsRunning()
    {
        return _client is not null;
    }
    
    public override void Restart()
    {
        RestartSimple(GetType().Name, _logger);
    }
    #endregion

    public Task<bool> DownloadAsync(string sourceUrl, string fileLocation, int timeoutMs = 5000)
    {
        throw new System.NotImplementedException();
    }

    public Task<string?> SendAsync(HttpRequestMessage requestMessage, int timeoutMs = 5000)
    {
        throw new System.NotImplementedException();
    }
}