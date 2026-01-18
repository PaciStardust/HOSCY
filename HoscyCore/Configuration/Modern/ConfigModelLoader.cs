using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace HoscyCore.Configuration.Modern;

public static class ConfigModelLoader
{
    public const string DEFAULT_FILE_NAME = "hoscy-config.json";

    /// <summary>
    /// Loads a ConfigModel from a file
    /// </summary>
    /// <returns>Null when no config could be loaded</returns>
    public static ConfigModel? TryLoad(string cfgFolder, string cfgFilename, ILogger logger)
    {
        var path = Path.Combine(cfgFolder, cfgFilename);
        logger.Information("Attempting to load Config at path {configPath}", path);
        try
        {
            if (!Directory.Exists(cfgFolder)) return null;
            if (!File.Exists(path)) return null;
            string configData = File.ReadAllText(path, Encoding.UTF8);
            TryCreateRawBackup(path + ".backup", configData, logger);
            var newData = JsonConvert.DeserializeObject<ConfigModel>(configData);
            if (newData is not null)
                return newData;
        }
        catch (JsonReaderException ex)
        {
            logger.Error(ex, "Unable to read JSON file at {configPath} correctly", path);
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error while reading JSON file at {configPath}", path);
            throw;
        }

        return null;
    }

    /// <summary>
    /// Attempt to save the config file
    /// </summary>
    /// <returns>Success</returns>
    public static bool TrySave(this ConfigModel model, string cfgFolder, string cfgFilename, ILogger logger)
    {
        var path = Path.Combine(cfgFolder, cfgFilename);
        logger.Information("Attempting to save Config at path {configPath}", path);
        try
        {
            if (!Directory.Exists(cfgFolder))
            {
                logger.Information("Config directory {directoryPath} not found, attempting creation", cfgFolder);
                Directory.CreateDirectory(cfgFolder);
                logger.Information("Created config directory {directoryPath}", cfgFolder);
            }

            var jsonText = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText(path, jsonText, Encoding.UTF8);
            logger.Information("Saved Config at path {configPath}", path);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "The config file was unable to be saved.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Writes the raw text of a loaded config into a file
    /// </summary>
    /// <returns>Success</returns>
    private static bool TryCreateRawBackup(string contents, string path, ILogger logger)
    {
        try
        {
            logger.Information("Attempting creation of backup of config file at {backupPath}", path);
            File.WriteAllText(path, contents, Encoding.UTF8);
            logger.Information("Succeeded creation of backup of config file at {backupPath}", path);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed creation of backup of config file at {backupPath}", path);
            return false;
        }
        return true;
    }

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
                if (config.Speech_Replacement_Partial.Count == 0) {
                    config.Speech_Replacement_Partial = [
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
                config.Speech_Replacement_Full.Add(new("box toggle", "[osc] [/avatar/parameters/ToolEnableBox [b]true \"self\"]"));
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
                config.Speech_Whisper_NoiseFilter = new() {
                    { "Laughing", "laugh" },
                    { "Popping", "pop" },
                    { "Whistling", "whistl" },
                    { "Sighing", "sigh" },
                    { "Humming", "hum" }
                };
            }}
        };

        var newestVersion = steps.Keys.Max();
        if (config.ConfigVersion == newestVersion)
        {
            logger.Information("Config is already at version {newestVersion}, skipping upgrade", newestVersion);
        }
        logger.Information("Config is at version {currentVersion}, newest is {newestVersion}, starting upgrade", config.ConfigVersion, newestVersion);

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