using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hoscy.Services.Api
{
    internal static class HoscyClient
    {
        private static readonly HttpClient _client;
        static HoscyClient()
        {
            _client = new(new SocketsHttpHandler()
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                UseProxy = false,
            });

            //Below is required for Github Access
            _client.DefaultRequestHeaders.UserAgent.Add(new("User-Agent", "request"));
        }

        #region Requests
        /// <summary>
        /// Wrapper for sending with the HTTPClient
        /// </summary>
        /// <param name="requestMessage">RequestMessage to send</param>
        /// <param name="timeout">Request timeout</param>
        /// <param name="notify">Notification window on error?</param>
        /// <returns>JSON response on success</returns>
        internal static async Task<string?> SendAsync(HttpRequestMessage requestMessage, int timeout = 5000, bool notify = true)
        {
            var identifier = GetRequestIdentifier();

            var startTime = DateTime.Now;
            Logger.Debug($"Sending {requestMessage.Method} to {requestMessage.RequestUri} ({identifier})");
            try
            {
                var cts = new CancellationTokenSource(timeout);
                var response = await _client.SendAsync(requestMessage, cts.Token);

                var jsonIn = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Request {identifier} has received status code \"{response.StatusCode}\" ({(int)response.StatusCode})" + (string.IsNullOrWhiteSpace(jsonIn) ? "" : $" ({jsonIn})"), notify: notify);
                    return null;
                }

                Logger.Debug($"Received data from request ({(DateTime.Now - startTime).TotalMilliseconds}ms): {identifier} => {jsonIn}");
                return jsonIn;
            }
            catch (Exception e)
            {
                if ((e is TaskCanceledException tce && tce.CancellationToken.IsCancellationRequested) || e is OperationCanceledException)
                    Logger.Warning($"Request {identifier} timed out");
                else
                    Logger.Error(e, "Failed to perform web request.", notify: notify);
            }
            return null;
        }

        /// <summary>
        /// Downloads a file and saves it to a location
        /// </summary>
        /// <param name="url">Url to download from</param>
        /// <param name="location">Location to save file to</param>
        /// <param name="timeout">Maximum timeout for reques</param>
        /// <param name="notify">Notify on error</param>
        /// <returns>Success?</returns>
        internal static async Task<bool> DownloadAsync(string url, string location, int timeout = 5000, bool notify = true)
        {
            var identifier = GetRequestIdentifier();

            var startTime = DateTime.Now;
            Logger.Debug($"Downloading file from {url} ({identifier})");

            try
            {
                var cts = new CancellationTokenSource(timeout);
                using var stream = await _client.GetStreamAsync(url, cts.Token);
                using var fStream = new FileStream(location, FileMode.OpenOrCreate);
                await stream.CopyToAsync(fStream);
                Logger.Debug($"Received file from request ({(DateTime.Now - startTime).TotalMilliseconds}ms): {identifier} => {location}");
                return true;
            }
            catch(Exception e) {
                if ((e is TaskCanceledException tce && tce.CancellationToken.IsCancellationRequested) || e is OperationCanceledException)
                    Logger.Warning($"Request {identifier} timed out");
                else
                    Logger.Error(e, "Failed to perform download", notify: notify);
            }
            return false;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Gets a unique identifier for the request
        /// </summary>
        /// <returns>Unique identifier</returns>
        private static string GetRequestIdentifier()
            => "R-" + Guid.NewGuid().ToString().Split('-')[0];
        #endregion
    }
}
