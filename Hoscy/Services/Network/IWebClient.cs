using System;
using System.Net.Http;
using System.Threading.Tasks;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Network;

public interface IWebClient : IStartStopService
{
    /// <summary>
    /// Sends a message and returns a response
    /// </summary>
    /// <param name="requestMessage">Message to send</param>
    /// <param name="timeoutMs">Timeout before cancellation</param>
    /// <returns>Null if failed</returns>
    public Task<string?> SendAsync(HttpRequestMessage requestMessage, int timeoutMs = 5000);

    /// <summary>
    /// Downloads a file
    /// </summary>
    /// <param name="sourceUrl">Url of file to download</param>
    /// <param name="fileLocation">Location to save file to</param>
    /// <param name="timeoutMs">Timeout before cancellation</param>
    /// <returns>Success</returns>
    public Task<bool> DownloadAsync(string sourceUrl, string fileLocation, int timeoutMs = 5000);

    /// <summary>
    /// Gets a unique identifier for a request
    /// </summary>
    protected static string GetRequestIdentifier()
    {
        return "R-" + Guid.NewGuid().ToString().Split('-')[0];
    }
}