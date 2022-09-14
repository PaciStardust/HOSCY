using Hoscy;
using Hoscy.Services.Speech;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Hoscy.Services.Api
{
    public class ApiClient
    {
        private Config.ConfigApiPresetModel? _preset;
        private HttpClient? _client;

        public bool LoadPreset(Config.ConfigApiPresetModel preset)
        {
            if (preset.Equals(_preset))
                return true;

            if (!preset.IsValid())
            {
                Logger.Error($"Did not reload ApiClient as preset \"{preset.Name}\" is invalid!", "ApiClient");
                Clear();
                return false;
            }

            try
            {
                var client = new HttpClient(new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                    UseProxy = false
                })
                {
                    BaseAddress = new Uri(preset.PostUrl),
                    Timeout = TimeSpan.FromMilliseconds(preset.ConnectionTimeout)
                };

                foreach (var headerInfo in preset.HeaderValues)
                {
                    if (!client.DefaultRequestHeaders.TryAddWithoutValidation(headerInfo.Key, headerInfo.Value))
                        Logger.Warning($"Skipped adding header info {headerInfo.Key} : {headerInfo.Value} as it was deemed invalid", "ApiClient");
                }

                _client = client;
                _preset = preset;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "ApiClient");
                Clear();
                return false;
            }
        }

        private async Task<string> Send(HttpContent content)
        {
            if (_preset == null || _client == null || !_preset.IsValid())
                return string.Empty;

            var identifier = "R-" + Math.Abs(content.GetHashCode());

            var startTime = DateTime.Now;
            Logger.Debug($"Sending to {_preset.PostUrl} ({identifier})", "ApiClient");
            var response = await _client.PostAsync("", content);
            var jsonIn = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error(jsonIn, "Translation");
                return string.Empty;
            }

            var result = TextProcessor.ExtractFromJson(_preset.ResultField, jsonIn);
            Logger.Debug($"Received data from request ({(DateTime.Now - startTime).TotalMilliseconds}ms): {identifier} => {jsonIn}", "ApiClient");
            return result;
        }

        public async Task<string> SendBytes(byte[] bytes)
        {
            if (_preset == null) return string.Empty;

            var content = new ByteArrayContent(bytes);
            if (string.IsNullOrWhiteSpace(_preset.ContentType) || !content.Headers.TryAddWithoutValidation("Content-Type", _preset.ContentType))
            {
                Logger.Error("Unable to send data to API as ContentType is invalid, are you using the Type suggested by the API's documentation?", "ApiClient");
                return string.Empty;
            }

            return await Send(content);
        }

        public async Task<string> SendText(string text)
        {
            if (_preset == null) return string.Empty;
            var jsonOut = ReplaceToken(_preset.JsonData, "[T]", text);

            if (_preset.JsonData == jsonOut)
            {
                Logger.Error("Unable to send data to data to API as JSON contains no token, have you made sure the JSON option contains \"[T]\"?", "ApiClient");
                return string.Empty;
            }

            return await Send(new StringContent(jsonOut, Encoding.UTF8, "application/json"));
        }

        public void Clear()
        {
            _client?.Dispose();
            _client = null;
            _preset = null;
        }

        private static string ReplaceToken(string text, string token, string value)
            => text.Replace(token, value);
    }
}
