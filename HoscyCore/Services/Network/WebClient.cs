using System.Diagnostics;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Network;

[LoadIntoDiContainer(typeof(IWebClient), Lifetime.Singleton)]
public class WebClient(ILogger logger)
    : StartStopServiceBase(logger.ForContext<WebClient>()), IWebClient
{
    private HttpClient? _client = null;

    #region Start / Stop
    protected override Res StartForService()
    {
        _logger.Debug("Starting internal HttpClient");
        _client = new HttpClient(new SocketsHttpHandler()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            UseProxy = false,
        });

        //Below is required for Github Access
        _client.DefaultRequestHeaders.UserAgent.Add(new("User-Agent", "request"));

        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForService()
    {
        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _client?.Dispose();
        _client = null;
    }

    protected override bool IsStarted()
        => _client is not null;
    protected override bool IsProcessing()
        => IsStarted();
    #endregion

    #region Functionality
    public async Task<Res> DownloadAsync(string sourceUrl, string fileLocation, int timeoutMs = 5000, CancellationToken? ctsExternal = null)
    {
        var identifier = IWebClient.GetRequestIdentifier();
        _logger.Debug("{identifier}: Downloading file from \"{url}\"", identifier);

        if (_client is null)
        {
            _logger.Error("{identifier}: Failed downloading, HttpClient is not initialized", identifier);
            return ResC.Fail(ResMsg.Err("Failed downloading, HttpClient is not initialized"));
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            var comboCts = ctsExternal is null ? cts : CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ctsExternal.Value);

            using var stream = await _client.GetStreamAsync(sourceUrl, comboCts.Token);
            using var fStream = new FileStream(fileLocation, FileMode.OpenOrCreate);
            await stream.CopyToAsync(fStream);
            _logger.Debug("{identifier}: Received file at path \"{fileLocation}\" from \"{sourceUrl}\" in {timePassed}ms",
                identifier, fileLocation, sourceUrl, sw.ElapsedMilliseconds);
        }
        catch(Exception ex)
        {
            if ((ex is TaskCanceledException tce && tce.CancellationToken.IsCancellationRequested) || ex is OperationCanceledException)
            {
                _logger.Warning("{identifier}: Download from \"{url}\" timed out after {timeout}ms",
                    identifier, sourceUrl, timeoutMs);
                return ResC.Fail(ResMsg.Err($"Download from \"{sourceUrl}\" timed out after {timeoutMs}ms"));
            }
            else
            {
                _logger.Error(ex, "{identifier}: Download from \"{url}\" failed", identifier, sourceUrl);
                return ResC.Fail(ResMsg.Err(ResMsg.FmtEx(ex, $"Download from \"{sourceUrl}\" failed")));
            }
        }

        return ResC.Ok();
    }

    public async Task<Res<string>> SendAsyncString(HttpRequestMessage requestMessage, int timeoutMs = 5000, CancellationToken? ctsExternal = null)
    {
        return await SendAsync(requestMessage, (con, ct) => con.ReadAsStringAsync(ct), timeoutMs, ctsExternal);
    }

    public async Task<Res<byte[]>> SendAsyncBytes(HttpRequestMessage requestMessage, int timeoutMs = 5000, CancellationToken? ctsExternal = null)
    {
        return await SendAsync(requestMessage, (con, ct) => con.ReadAsByteArrayAsync(ct), timeoutMs, ctsExternal);
    }

    private async Task<Res<T>> SendAsync<T>(HttpRequestMessage requestMessage, Func<HttpContent, CancellationToken, Task<T>> retrieveTask, int timeoutMs = 5000, CancellationToken? ctsExternal = null) where T : notnull
    {
        var identifier = IWebClient.GetRequestIdentifier();
        var logType = typeof(T).Name;
        _logger.Verbose("{identifier} => Sending {T} \"{requestMethod}\" request to \"{requestUri}\"",
            identifier, logType, requestMessage.Method, requestMessage.RequestUri);

        if (_client is null)
        {
            _logger.Error("{identifier}: Failed sending, HttpClient is not initialized", identifier);
            return ResC.TFail<T>(ResMsg.Err("Failed sending, HttpClient is not initialized"));
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            var comboCts = ctsExternal is null ? cts : CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ctsExternal.Value);

            var response = await _client.SendAsync(requestMessage, comboCts.Token);

            var result = await retrieveTask(response.Content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var jsonRes = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.Error("{identifier}: Request has received status code \"{responseStatusCode}\" ({intResponseStatusCode}) \"{responseJson}\"",
                    identifier, response.StatusCode, (int)response.StatusCode, string.IsNullOrWhiteSpace(jsonRes) ? "" : $" ({jsonRes})");
                return ResC.TFail<T>(ResMsg.Err($"Request failed with status code {response.StatusCode}"));
            }

            _logger.Verbose("{identifier}: Received data from request in {timePassed}ms => {jsonIn}",
                identifier, sw.ElapsedMilliseconds, result.ToString());
            return ResC.TOk(result);
        }
        catch (Exception ex)
        {
            if ((ex is TaskCanceledException tce && tce.CancellationToken.IsCancellationRequested) || ex is OperationCanceledException)
            {
                _logger.Warning("{identifier}: Request to \"{requestUri}\" timed out after {timeout}ms",
                    identifier, requestMessage.RequestUri, timeoutMs);
                return ResC.TFail<T>(ResMsg.Err($"Request to \"{requestMessage.RequestUri}\" timed out after {timeoutMs}ms"));
            }
            else
            {
                _logger.Error(ex, "{identifier}: Request to \"{requestUri}\" failed",
                    identifier, requestMessage.RequestUri);
                return ResC.TFail<T>(ResMsg.Err(ResMsg.FmtEx(ex, $"Request to \"{requestMessage.RequestUri}\" failed")));
            }
        }
    }
    #endregion
}