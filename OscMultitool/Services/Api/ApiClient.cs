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
        private Config.ApiPresetModel? _preset;

        public bool LoadPreset(Config.ApiPresetModel preset)
        {
            if (preset.Equals(_preset))
                return true;

            Clear();

            if (!preset.IsValid())
            {
                Logger.Error($"Did not reload ApiClient as preset \"{preset.Name}\" is invalid!");
                return false;
            }

            _preset = preset;
            return true;
        }

        private async Task<string?> Send(HttpContent content)
        {
            if (_preset == null || !_preset.IsValid())
                return null;

            AddHeaders(content);

            var jsonIn = await HoscyClient.RequestAsync(_preset.PostUrl, content, _preset.ConnectionTimeout);

            if (jsonIn == null)
                return null;

            var result = TextProcessor.ExtractFromJson(_preset.ResultField, jsonIn);
            return result;
        }

        public async Task<string?> SendBytes(byte[] bytes)
        {
            if (_preset == null) return null;

            var content = new ByteArrayContent(bytes);
            if (string.IsNullOrWhiteSpace(_preset.ContentType) || !content.Headers.TryAddWithoutValidation("Content-Type", _preset.ContentType))
            {
                Logger.Error("Unable to send data to API as ContentType is invalid, are you using the Type suggested by the API's documentation?");
                return string.Empty;
            }

            return await Send(content);
        }

        public async Task<string?> SendText(string text)
        {
            if (_preset == null) return string.Empty;
            var jsonOut = ReplaceToken(_preset.JsonData, "[T]", text);

            if (_preset.JsonData == jsonOut)
            {
                Logger.Error("Unable to send data to data to API as JSON contains no token, have you made sure the JSON option contains \"[T]\"?");
                return string.Empty;
            }

            return await Send(new StringContent(jsonOut, Encoding.UTF8, "application/json"));
        }

        private void AddHeaders(HttpContent content)
        {
            if (_preset == null)
                return;

            foreach (var headerInfo in _preset.HeaderValues)
            {
                if (!content.Headers.TryAddWithoutValidation(headerInfo.Key, headerInfo.Value))
                {
                    Logger.Error($"Skipped adding header info \"{headerInfo.Key} : {headerInfo.Value}\". As it was deemed invalid, it will be removed");
                    _preset.HeaderValues.Remove(headerInfo.Key);
                }
            }
        }

        public void Clear()
            => _preset = null;

        private static string ReplaceToken(string text, string token, string value)
            => text.Replace(token, value);
    }
}
