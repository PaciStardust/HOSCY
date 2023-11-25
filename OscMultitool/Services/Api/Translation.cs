using System.Threading.Tasks;

namespace Hoscy.Services.Api
{
    internal static class Translator
    {
        private static readonly ApiClient _client = new();

        static Translator()
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

            var index = Config.Api.GetPresetIndex(Config.Api.TranslationPreset);
            if (index == -1)
            {
                Logger.Warning("Attempted to use a non existant preset");
                return;
            }
            _client.LoadPreset(Config.Api.Presets[index]);
        }
    }
}
