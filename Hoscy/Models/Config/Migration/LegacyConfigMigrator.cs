using System.Collections.Generic;

namespace Hoscy.Models.Config.Migration;

internal static class OldConfigMigrator
{
    internal static LegacyConfigModel Upgrade(this LegacyConfigModel config)
    {
        if (config.ConfigVersion < 1) //contains ifs to ensure old configs dont get these again
        {
            if (config.Speech.NoiseFilter.Count == 0)
            {
                config.Speech.NoiseFilter.AddRange(
                [
                    "the",
                    "and",
                    "einen"
                ]);
            }

            if (config.Speech.Replacements.Count == 0)
            {
                config.Speech.Replacements.AddRange(
                [
                    new("exclamation mark", "!", false),
                    new("question mark", "?", false),
                    new("colon", ":", false),
                    new("semicolon", ";", false),
                    new("open parenthesis", "(", false),
                    new("closed parenthesis", ")", false),
                    new("open bracket", "(", false),
                    new("closed bracket", ")", false),
                    new("minus", "-", false),
                    new("plus", "+", false),
                    new("slash", "/", false),
                    new("backslash", "\\", false),
                    new("hashtag", "#", false),
                    new("asterisk", "*", false)
                ]);
            }
        }

        if (config.ConfigVersion < 2)
        {
            config.Speech.Shortcuts.Add(new("box toggle", "[osc] [/avatar/parameters/ToolEnableBox [b]true \"self\"]"));

            config.Api.Presets.AddRange(new List<LegacyApiPresetModel>()
            {
                new()
                {
                    Name = "Example - Azure to DE",
                    TargetUrl = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=de",
                    ResultField = "text",
                    SentData = @"[{""Text"" : ""[T]""}]",
                    ContentType = "application/json",
                    HeaderValues = new()
                    {
                        { "Ocp-Apim-Subscription-Key", "[YOUR KEY]" },
                        { "Ocp-Apim-Subscription-Region", "[YOUR REGION]" }
                    }
                },

                new()
                {
                    Name = "Example - Azure Recognition",
                    TargetUrl = "https://northeurope.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US",
                    ResultField = "Display Text",
                    SentData = string.Empty,
                    ContentType = "audio/wav; codecs=audio/pcm; samplerate=16000",
                    HeaderValues = 
                    {
                        { "Ocp-Apim-Subscription-Key", "[YOUR KEY]" },
                        { "Accept", "true" }
                    }
                },

                new()
                {
                    Name = "Example - DeepL to DE",
                    TargetUrl = "https://api-free.deepl.com/v2/translate",
                    ResultField = "text",
                    SentData = "text=[T]&target_lang=DE",
                    ContentType = "application/x-www-form-urlencoded",
                    Authorization = "DeepL-Auth-Key [YOUR KEY]"
                }
            });
        }

        if (config.ConfigVersion < 4)
        {
            if (config.Debug.LogFilters.Count == 0)
            {
                config.Debug.LogFilters.AddRange(
                [
                    new("VRC Angular", "/Angular"),
                    new("VRC Grounded", "/Grounded"),
                    new("VRC Velocity", "/Velocity"),
                    new("VRC Upright", "/Upright"),
                    new("VRC Voice", "/Voice"),
                    new("VRC Viseme", "/Viseme"),
                    new("VRC Gesture", "/Gesture"),
                    new("VRC Angle", "_Angle"),
                    new("VRC Stretch", "_Stretch"),
                    new("Notification Timeout", "Notification timeout was"),
                    new("Notification Override", "Did not override")
                ]);
            }
        }

        if (config.ConfigVersion < 5)
        {
            if (config.Speech.WhisperNoiseFilter.Count == 0)
            {
                config.Speech.WhisperNoiseFilter = new()
                {
                    { "Laughing", "laugh" },
                    { "Popping", "pop" },
                    { "Whistling", "whistl" },
                    { "Sighing", "sigh" },
                    { "Humming", "hum" }
                };
            }
        }

        config.ConfigVersion = 5;
        return config;
    }
}