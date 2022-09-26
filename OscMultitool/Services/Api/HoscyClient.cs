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

        private static string GetRequestIdentifier(HttpContent content)
            => $"R-{Math.Abs(content.GetHashCode())}";
    }
}
