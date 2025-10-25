using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using Serilog;

namespace Hoscy.Services.Network;

[LoadIntoDiContainer(typeof(IWebClient), Lifetime.Singleton)]
public class WebClient(ILogger logger, IBackToFrontNotifyService notify) : StartStopServiceBase, IWebClient
{
    private readonly ILogger _logger = logger.ForContext<WebClient>();
    private readonly IBackToFrontNotifyService _notify = notify;
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

    #region Functionality
    public async Task<bool> DownloadAsync(string sourceUrl, string fileLocation, int timeoutMs = 5000)
    {
        var identifier = IWebClient.GetRequestIdentifier();
        _logger.Debug("{identifier}: Downloading file from {url}", identifier);

        if (_client is null)
        {
            _logger.Error("{identifier}: Failed downloading, HttpClient is not initialized", identifier);
            return false;
        }

        var startTime = DateTime.Now;
        try
        {
            var cts = new CancellationTokenSource(timeoutMs);
            using var stream = await _client.GetStreamAsync(sourceUrl, cts.Token);
            using var fStream = new FileStream(fileLocation, FileMode.OpenOrCreate);
            await stream.CopyToAsync(fStream);
            _logger.Debug("{identifier}: Received file at path {fileLocation} from {sourceUrl} in {timePassed}ms", identifier, fileLocation, sourceUrl, (DateTime.Now - startTime).TotalMilliseconds);
            return true;
        }
        catch(Exception ex) {
            if ((ex is TaskCanceledException tce && tce.CancellationToken.IsCancellationRequested) || ex is OperationCanceledException)
            {
                _logger.Warning("{identifier}: Download from {url} timed out after {timeout}ms", identifier, sourceUrl, timeoutMs);
                _notify.SendWarning($"Download from {sourceUrl} timed out after {timeoutMs}ms");
            }
            else
            {
                _logger.Error(ex, "{identifier}: Download from {url} failed", identifier, sourceUrl);
                _notify.SendError("Failed Download", $"Download from {sourceUrl} failed, see logs for more details", ex); //todo: more details?
            }
        }
        return false;
    }

    public async Task<string?> SendAsync(HttpRequestMessage requestMessage, int timeoutMs = 5000)
    {
        var identifier = IWebClient.GetRequestIdentifier();
        _logger.Debug("{identifier} => Sending {requestMethod} request to {requestUri}", identifier, requestMessage.Method, requestMessage.RequestUri);

        if (_client is null)
        {
            _logger.Error("{identifier}: Failed sending, HttpClient is not initialized", identifier);
            return null;
        }

        var startTime = DateTime.Now;
        try
        {
            var cts = new CancellationTokenSource(timeoutMs);
            var response = await _client.SendAsync(requestMessage, cts.Token);
            var jsonIn = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("{identifier}: Request has received status code \"{responseStatusCode}\" ({intResponseStatusCode}){responseJson}",
                    identifier, response.StatusCode, (int)response.StatusCode, string.IsNullOrWhiteSpace(jsonIn) ? "" : $" ({jsonIn})");
                _notify.SendError("Failed Web Request", $"Request to {requestMessage.RequestUri} failed, see logs for more details"); //todo: more details?
                return null;
            }

            _logger.Debug("{identifier}: Received data from request in {timePassed}ms => {jsonIn}", identifier, (DateTime.Now - startTime).TotalMilliseconds, jsonIn);
            return jsonIn;
        }
        catch (Exception ex)
        {
            if ((ex is TaskCanceledException tce && tce.CancellationToken.IsCancellationRequested) || ex is OperationCanceledException)
            {
                _logger.Warning("{identifier}: Request to {requestUri} timed out after {timeout}ms", identifier, requestMessage.RequestUri, timeoutMs);
                _notify.SendWarning($"Request to {requestMessage.RequestUri} timed out after {timeoutMs}ms");
            }
            else
            {
                _logger.Error(ex, "{identifier}: Request to {requestUri} failed", identifier, requestMessage.RequestUri);
                _notify.SendError("Failed Web Request", $"Request to {requestMessage.RequestUri} failed, see logs for more details", ex); //todo: more details?
            }
        }
        return null;
    }
    #endregion
}