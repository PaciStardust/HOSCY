using Hoscy.Services.Speech;
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

        /// <summary>
        /// Either does a post or get
        /// </summary>
        /// <param name="url">Target URL</param>
        /// <param name="content">Content, if this is null, a get will be performed</param>
        /// <param name="timeout">Timeout in ms</param>
        /// <returns>Response, if null this failed</returns>
        public static async Task<string?> RequestAsync(string url, HttpContent? content, int timeout = 5000, bool notify = true)
        {
            var identifier = GetRequestIdentifier();

            var startTime = DateTime.Now;
            Logger.Debug($"{(content == null ? "Getting from" : "Posting to")} {url} ({identifier})");
            try
            {
                var cts = new CancellationTokenSource(timeout);
                var response = content == null
                    ? await _client.GetAsync(url, cts.Token)
                    : await _client.PostAsync(url, content, cts.Token);

                var jsonIn = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Request {identifier} has received status code \"{response.StatusCode}\"" + (string.IsNullOrWhiteSpace(jsonIn) ? "" : $" ({jsonIn})"), notify:notify);
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
                    Logger.Error(e, notify: notify);
            }
            return null;
        }

        public static void CheckForUpdates()
           => Task.Run(async() => await CheckForUpdatesInternal()).ConfigureAwait(false);
        private static async Task CheckForUpdatesInternal()
        {
            var currVer = Config.GetVersion();
            Logger.PInfo("Attempting to check for newest HOSCY version, current is " + currVer);

            var res = await RequestAsync(Config.GithubLatest, null, notify: false);
            if (res == null)
            {
                Logger.Warning("Failed to grab version number from GitHub");
                return;
            }

            var newVer = TextProcessor.ExtractFromJson("tag_name", res);
            if (newVer != null && currVer != newVer)
            {
                Logger.Warning($"New version available (Latest is {newVer})");
                Logger.OpenNotificationWindow("New version available", "A new version of HOSCY is available", $"Current: {currVer}\nLatest:{newVer}");
            }
            else
                Logger.Info("HOSCY is up to date");
        }

        /// <summary>
        /// Gets a unique identifier for the request
        /// </summary>
        /// <returns>Unique identifier</returns>
        private static string GetRequestIdentifier()
            => "R-" + Guid.NewGuid().ToString().Split('-')[0];
    }
}
