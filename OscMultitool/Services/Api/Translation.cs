using System.Threading.Tasks;

namespace Hoscy.Services.Api
{
    internal static class Translation
    {
        private static readonly ApiClient _client = new();

        static Translation()
        {
            ReloadClient();
        }

        internal async static Task<string> Translate(string text)
        {
            text = text.Replace("\"", "");
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length > Config.Api.TranslationMaxTextLength)
            {
                if (Config.Api.TranslationSkipLongerMessages)
                    return text;
                else
                    text = text[..Config.Api.TranslationMaxTextLength];
            }

            Logger.Log("Requesting translation of text: " + text);
            var result = await _client.SendText(text);

            if (result == null || string.IsNullOrWhiteSpace(result))
                return text;

            return result;
        }

        internal static void ReloadClient()
        {
            Logger.PInfo("Performing reload of Translation");

            var preset = Config.Api.GetPreset(Config.Api.TranslationPreset);
            if (preset == null)
            {
                Logger.Warning("Attempted to use a non existant preset");
                return;
            }
            _client.LoadPreset(preset);
        }
    }
}
