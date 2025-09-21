using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace Hoscy.Models.Config;

public static class ConfigModelLoader
{
    /// <summary>
    /// Upgrades a ConfigModel to the newest version
    /// </summary>
    public static ConfigModel Upgrade(this ConfigModel config, ILogger logger)
    {
        Dictionary<int, Action> steps = new()
        {
            { 1, () => {
                if (config.Speech_Replacement_NoiseFilter.Count == 0)
                {
                    config.Speech_Replacement_NoiseFilter = [
                        "the",
                        "and",
                        "einen"
                    ];
                }
                if (config.Speech_Replacement_Replacements.Count == 0) {
                    config.Speech_Replacement_Replacements = [
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
                    ];
                }
            }},
            {2, () => {
                config.Speech_Replacement_Shortcuts.Add(new("box toggle", "[osc] [/avatar/parameters/ToolEnableBox [b]true \"self\"]"));
                if (config.ApiCommunication_Presets.Count > 0) return;
                config.ApiCommunication_Presets = [
                    new()
                    {
                        Name = "Example - Azure to DE",
                        TargetUrl = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=de",
                        ResultField = "text",
                        SentData = @"[{""Text"" : ""[T]""}]",
                        ContentType = "application/json",
                        HeaderValues = new()
                        {
                            new("Ocp-Apim-Subscription-Key", "[YOUR KEY]"),
                            new("Ocp-Apim-Subscription-Region", "[YOUR REGION]")
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
                            new( "Ocp-Apim-Subscription-Key", "[YOUR KEY]" ),
                            new( "Accept", "true" )
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
                ];
            }},
            {4, () => {
                if (config.Logger_Filters.Count != 0) return;
                config.Logger_Filters = [
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
                ];
            }},
            {5, () => {
                if (config.Speech_Whisper_NoiseFilter.Count > 0) return;
                config.Speech_Whisper_NoiseFilter = [
                    new( "Laughing", "laugh" ),
                    new( "Popping", "pop" ),
                    new( "Whistling", "whistl" ),
                    new( "Sighing", "sigh" ),
                    new( "Humming", "hum" )
                ];
            }}
        };

        var newestVersion = steps.Keys.Max();
        if (config.ConfigVersion == newestVersion)
        {
            logger.Information("Config is already at version {newestVersion}, skipping upgrade", newestVersion);
        }
        logger.Information("Config is at version {currentVersion}, newst is {newestVersion}, starting upgrade", config.ConfigVersion, newestVersion);

        foreach (var (version, action) in steps.OrderBy(x => x.Key))
        {
            logger.Information("Upgrading config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
            try
            {
                action();
                config.ConfigVersion = version;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to upgrade config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
                throw;
            }
            logger.Information("Upgraded config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
        }
        logger.Information("Finished upgrading config to version {newestVersion}", newestVersion);
        return config;
    }
}