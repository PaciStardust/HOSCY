using Hoscy.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Hoscy
{
    internal static class Config
    {
        public static ConfigModel Data { get; private set; }
        public static ConfigOscModel Osc => Data.Osc;
        public static ConfigSpeechModel Speech => Data.Speech;
        public static ConfigTextboxModel Textbox => Data.Textbox;
        public static ConfigInputModel Input => Data.Input;
        public static ConfigApiModel Api => Data.Api;
        public static ConfigLoggerModel Debug => Data.Debug;

        #region Saving and Loading
        static Config()
        {
            try
            {
                if (!Directory.Exists(Utils.PathConfigFolder))
                    Directory.CreateDirectory(Utils.PathConfigFolder);

                if (!Directory.Exists(Utils.PathModels))
                    Directory.CreateDirectory(Utils.PathModels);

                string configData = File.ReadAllText(Utils.PathConfigFile, Encoding.UTF8);
                Data = JsonConvert.DeserializeObject<ConfigModel>(configData) ?? new();
                TryLoadFolderModels();
            }
            catch
            {
                Data = new();
            }

            UpdateConfig(Data);

            if (!Directory.Exists(Utils.PathConfigFolder))
                MessageBox.Show("Failed to create config directory, please check your antivirus and your permissions", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Saves the config file
        /// </summary>
        internal static void SaveConfig()
        {
            try
            {
                var jsonText = JsonConvert.SerializeObject(Data ?? new(), Formatting.Indented);
                File.WriteAllText(Utils.PathConfigFile, jsonText, Encoding.UTF8);
                Logger.Info($"Saved config file at " + Utils.PathConfigFile);
            }
            catch (Exception e)
            {
                Logger.Error(e, "The config file was unable to be saved.", notify: false);
            }
        }

        internal static void BackupFile(string path)
        {
            try
            {
                var fileText = File.ReadAllText(path, Encoding.UTF8);
                File.WriteAllText(path + ".backup", fileText, Encoding.UTF8);
                Logger.Info($"Backed up file {path}");
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to backe up file {path}", notify: false);
            }
        }

        /// <summary>
        /// Tries loading in models from the model folder
        /// </summary>
        private static void TryLoadFolderModels() //todo: [WHISPER] add whisper loading
        {
            var foldersNames = Directory.GetDirectories(Utils.PathModels);

            foreach (var folderName in foldersNames)
            {
                var contentFolder = Utils.GetActualContentFolder(folderName);

                if (string.IsNullOrWhiteSpace(contentFolder))
                    continue;

                var folderNameSplit = folderName.Split("\\")[^1];
                if (string.IsNullOrWhiteSpace(folderNameSplit))
                    continue;

                Speech.VoskModels[folderNameSplit] = contentFolder;
            }
        }
        #endregion

        #region Utility
        /// <summary>
        /// This exists as newtonsoft seems to have an issue with my way of creating a default config
        /// </summary>
        private static void UpdateConfig(ConfigModel config)
        {
            if (config.ConfigVersion < 1) //contains ifs to ensure old configs dont get these again
            {
                if (config.Speech.NoiseFilter.Count == 0)
                {
                    config.Speech.NoiseFilter.AddRange(new List<string>()
                    {
                        "the",
                        "and",
                        "einen"
                    });
                }

                if (config.Speech.Replacements.Count == 0)
                {
                    config.Speech.Replacements.AddRange(new List<ReplacementDataModel>()
                    {
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
                    });
                }
            }

            if (config.ConfigVersion < 2)
            {
                config.Speech.Shortcuts.Add(new("box toggle", "[osc] [/avatar/parameters/ToolEnableBox [b]true \"self\"]"));

                config.Api.Presets.AddRange(new List<ApiPresetModel>()
                {
                    new ApiPresetModel()
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

                    new ApiPresetModel()
                    {
                        Name = "Example - Azure Recognition",
                        TargetUrl = "https://northeurope.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US",
                        ResultField = "Display Text",
                        SentData = string.Empty,
                        ContentType = "audio/wav; codecs=audio/pcm; samplerate=16000",
                        HeaderValues = new()
                        {
                            { "Ocp-Apim-Subscription-Key", "[YOUR KEY]" },
                            { "Accept", "true" }
                        }
                    },

                    new ApiPresetModel()
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
                    config.Debug.LogFilters.AddRange(new List<FilterModel>()
                    {
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
                    });
                }
            }

            config.ConfigVersion = 4;
        }
        #endregion
    }
}
