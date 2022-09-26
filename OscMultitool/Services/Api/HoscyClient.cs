using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hoscy.Services.Api
{
    public static class HoscyClient
    {
        private static readonly HttpClient _client = new(new SocketsHttpHandler()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            UseProxy = false
        });

        /// <summary>
        /// Make a post request
        /// </summary>
        /// <param name="url">URL of the request</param>
        /// <param name="content">Content of the request</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>Null if request failed</returns>
        public static async Task<string?> PostAsync(string url, HttpContent content, int timeout)
        {
            var identifier = GetRequestIdentifier(content);

            var startTime = DateTime.Now;
            Logger.Debug($"Posting to {url} ({identifier})");
            try
            {
                var cts = new CancellationTokenSource(timeout);
                var response = await _client.PostAsync(url, content, cts.Token);

                var jsonIn = await response.Content.ReadAsStringAsync(CancellationToken.None);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Request {identifier} has received status code \"{response.StatusCode}\"" + (string.IsNullOrWhiteSpace(jsonIn) ? "" : $" ({jsonIn})"));
                    return null;
                }

                Logger.Debug($"Received data from request ({(DateTime.Now - startTime).TotalMilliseconds}ms): {identifier} => {jsonIn}");
                return jsonIn;
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException tce && tce.CancellationToken.IsCancellationRequested)
                    Logger.Warning($"Request {identifier} timed out");
                else
                    Logger.Error(e);
            }
            return null;
        }

        /// <summary>
        /// Gets a unique identifier for the request
        /// </summary>
        /// <param name="content">Content of the request</param>
        /// <returns>Unique identifier</returns>
        private static string GetRequestIdentifier(HttpContent content)
            => $"R-{Math.Abs(content.GetHashCode())}";
    }
}
