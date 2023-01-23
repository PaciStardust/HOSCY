using Hoscy.Services.Speech;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hoscy.Services.Api
{
    public static class HoscyClient
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
        public static async Task<string?> SendAsync(HttpRequestMessage requestMessage, int timeout = 5000, bool notify = true)
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
        #endregion

        #region Update Checking
        /// <summary>
        /// Checks if there is an update available, displays a window containing changelog
        /// </summary>
        public static void CheckForUpdates()
           => App.RunWithoutAwait(CheckForUpdatesInternal());
        /// <summary>
        /// Internal version to avoid async hell
        /// </summary>
        private static async Task CheckForUpdatesInternal()
        {
            var currVer = Config.GetVersion();
            Logger.PInfo("Attempting to check for newest HOSCY version, current is " + currVer);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, Config.GithubLatest);

            var res = await SendAsync(requestMessage, notify: false);
            if (res == null)
            {
                Logger.Warning("Failed to grab version number from GitHub");
                return;
            }

            var newVer = TextProcessor.ExtractFromJson("tag_name", res);
            var newBody = TextProcessor.ExtractFromJson("body", res);
            if (newVer != null && currVer != newVer)
            {
                Logger.Warning($"New version available (Latest is {newVer})");

                var notifText = $"Please update by running the \"updater.ps1\" file in the HOSCY folder or by redownloading from GitHub\n\nCurrent: {currVer}\nLatest: {newVer}{(string.IsNullOrWhiteSpace(newBody) ? string.Empty : $"\n\n{newBody}")}";
                Logger.OpenNotificationWindow("New version available", "A new version of HOSCY is available", notifText, true);
            }
            else
                Logger.Info("HOSCY is up to date");
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
