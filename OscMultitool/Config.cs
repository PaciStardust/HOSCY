using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Hoscy
{
    public static class Config
    {
        public static ConfigModel Data { get; private set; }
        public static ConfigOscModel Osc => Data.Osc;
        public static ConfigSpeechModel Speech => Data.Speech;
        public static ConfigTextboxModel Textbox => Data.Textbox;
        public static ConfigInputModel Input => Data.Input;
        public static ConfigApiModel Api => Data.Api;
        public static ConfigLoggerModel Debug => Data.Debug;

        public static string Github => "https://github.com/PaciStardust/HOSCY";
        public static string GithubLatest => "https://api.github.com/repos/pacistardust/hoscy/releases/latest";

        public static string ResourcePath { get; private set; }
        public static string ConfigPath { get; private set; }
        public static string LogPath { get; private set; }

        #region Saving and Loading
        static Config()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();

            ResourcePath = Path.GetFullPath(Path.Combine(assemblyDirectory, "config"));
            ConfigPath = Path.GetFullPath(Path.Combine(ResourcePath, "config.json"));
            LogPath = Path.GetFullPath(Path.Combine(ResourcePath, "log.txt"));

            try
            {
                if (!Directory.Exists(ResourcePath))
                    Directory.CreateDirectory(ResourcePath);

                string configData = File.ReadAllText(ConfigPath, Encoding.UTF8);
                Data = JsonConvert.DeserializeObject<ConfigModel>(configData) ?? new();
            }
            catch
            {
                Data = GetDefaultConfig();
            }

            if (!Directory.Exists(ResourcePath))
                MessageBox.Show("Failed to create config directory, please check your antivirus and your permissions", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Saves the config file
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Data ?? new(), Formatting.Indented));
                Logger.PInfo("Saved config file at " + ConfigPath);
            }
            catch (Exception e)
            {
                Logger.Error(e, "The config file was unable to be saved.", notify:false);
            }
        }
        #endregion

        #region Utility
        public static int MinMax(int value, int min, int max)
            => Math.Max(Math.Min(max, value), min);
        public static float MinMax(float value, float min, float max)
            => Math.Max(Math.Min(max, value), min);

        public static string GetVersion()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            return "v." + (assembly != null ? FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion : "Version Unknown");
        }

        /// <summary>
        /// This exists as newtonsoft seems to have an issue with my way of creating a default config
        /// </summary>
        private static ConfigModel GetDefaultConfig()
        {
            var config = new ConfigModel();

            config.Speech.NoiseFilter.AddRange(new List<string>()
            {
                "the",
                "and",
                "einen"
            });

            config.Debug.LogFilter.AddRange(new List<string>()
            {
                "/angular",
                "/grounded",
                "/velocity",
                "/upright",
                "/voice",
                "/viseme",
                "/gesture",
                "_angle",
                "_stretch"
            });

            config.Speech.Replacements.AddRange(new List<ReplacementModel>()
            {
                new("exclamation mark", "!"),
                new("question mark", "?"),
                new("colon", ":"),
                new("semicolon", ";"),
                new("open parenthesis", "("),
                new("closed parenthesis", ")"),
                new("open bracket", "("),
                new("closed bracket", ")"),
                new("minus", "-"),
                new("plus", "+"),
                new("slash", "/"),
                new("backslash", "\\"),
                new("hashtag", "#"),
                new("asterisk", "*")
            });

            return config;
        }

        #endregion

        #region Models
        /// <summary>
        /// Model for storing all config data
        /// </summary>
        public class ConfigModel
        {
            public ConfigOscModel Osc { get; init; } = new();
            public ConfigSpeechModel Speech { get; init; } = new();
            public ConfigTextboxModel Textbox { get; init; } = new();
            public ConfigInputModel Input { get; init; } = new();
            public ConfigApiModel Api { get; init; } = new();
            public ConfigLoggerModel Debug { get; init; } = new();
        }

        /// <summary>
        /// Model for storing all OSC related data (Ports, IPs, Addresses, Filters)
        /// </summary>
        public class ConfigOscModel
        {
            public string Ip { get; set; } = "127.0.0.1";
            public int Port { get; set; } = 9000;
            public int PortListen { get; set; } = 9001;
            public string AddressManualMute { get; set; } = "/avatar/parameters/ToolMute";
            public string AddressManualSkipSpeech { get; set; } = "/avatar/parameters/ToolSkipSpeech";
            public string AddressManualSkipBox { get; set; } = "/avatar/parameters/ToolSkipBox";
            public string AddressEnableReplacements { get; set; } = "/avatar/parameters/ToolEnableReplacements";
            public string AddressEnableTextbox { get; set; } = "/avatar/parameters/ToolEnableBox";
            public string AddressEnableTts { get; set; } = "/avatar/parameters/ToolEnableTts";
            public string AddressEnableAutoMute { get; set; } = "/avatar/parameters/ToolEnableAutoMute";
            public string AddressListeningIndicator { get; set; } = "/avatar/parameters/MicListening";
            public string AddressAddTextbox { get; set; } = "/hoscy/message";
            public string AddressAddTts { get; set; } = "/hoscy/tts";
            public string AddressAddNotification { get; set; } = "/hoscy/notification";
            public List<OscRoutingFilterModel> RoutingFilters { get; set; } = new();
        }

        /// <summary>
        /// Model for storing all Speech related data (TTS, Models, Device Options)
        /// </summary>
        public class ConfigSpeechModel
        {
            //Usage
            public bool UseTextbox { get; set; } = true;
            public bool UseTts { get; set; } = false;
            public string MicId { get; set; } = string.Empty;
            public bool StartUnmuted { get; set; } = true;
            public bool MuteOnVrcMute { get; set; } = true;

            public string ModelName { get; set; } = string.Empty;

            //TTS
            public string TtsId { get; set; } = string.Empty;
            public string SpeakerId { get; set; } = string.Empty;
            public float SpeakerVolume
            {
                get { return _speakerVolume; }
                set { _speakerVolume = MinMax(value, 0, 1); }
            }
            private float _speakerVolume = 0.5f;
            public int MaxLenTtsString
            {
                get { return _maxLenTtsString; }
                set { _maxLenTtsString = MinMax(value, 1, 99999); }
            }
            private int _maxLenTtsString = 500;
            public bool SkipLongerMessages { get; set; } = true;

            //Vosk
            public Dictionary<string, string> VoskModels { get; set; } = new();
            public string VoskModelCurrent { get; set; } = string.Empty;
            public int VoskTimeout
            {
                get { return _voskTimeout; }
                set { _voskTimeout = MinMax(value, 500, 30000); }
            }
            private int _voskTimeout = 3000;

            //Windows
            public string WinModelId { get; set; } = string.Empty;

            //Replacement
            public List<string> NoiseFilter { get; set; } = new();
            public bool IgnoreCaps { get; set; } = true;
            public bool RemoveFullStop { get; set; } = true;
            public bool UseReplacements { get; set; } = true;
            public List<ReplacementModel> Shortcuts { get; set; } = new();
            public List<ReplacementModel> Replacements { get; set; } = new();
        }

        /// <summary>
        /// Model for storing all Textbox related data (Timeouts, MaxLength)
        /// </summary>
        public class ConfigTextboxModel
        {
            public int MaxLength
            {
                get { return _maxLength; }
                set { _maxLength = MinMax(value, 50, 130); }
            }
            private int _maxLength = 130;
            public int TimeoutMultiplier
            {
                get { return _timeoutMultiplier; }
                set { _timeoutMultiplier = MinMax(value, 250, 10000); }
            }
            private int _timeoutMultiplier = 1250;

            public int MinimumTimeout
            {
                get { return _minimumTimeout; }
                set { _minimumTimeout = MinMax(value, 1000, 30000); }
            }
            private int _minimumTimeout = 3000;

            public int DefaultTimeout
            {
                get { return _defaultTimeout; }
                set { _defaultTimeout = MinMax(value, 1000, 30000); }
            }
            private int _defaultTimeout = 5000;
            public bool DynamicTimeout { get; set; } = true;
            public bool AutomaticClearNotification { get; set; } = true;
            public bool AutomaticClearMessage { get; set; } = false;
            public bool UseIndicatorWithoutBox { get; set; } = false;
            public bool SoundOnMessage { get; set; } = true;
            public bool SoundOnNotification { get; set; } = false;

            //Media
            public bool MediaShowStatus { get; set; } = false;
            public string MediaPlayingVerb { get; set; } = "Playing";
            public bool MediaAddAlbum { get; set; } = false;

        }

        /// <summary>
        /// Model for all API related data
        /// </summary>
        public class ConfigApiModel
        {
            //General
            public List<ApiPresetModel> Presets { get; set; } = new();

            //Recognition
            public string RecognitionPreset { get; set; } = string.Empty;
            public int RecognitionMaxRecordingTime
            {
                get { return _recognitionMaxRecordingTime; }
                set { _recognitionMaxRecordingTime = MinMax(value, 1, 300); }
            }
            private int _recognitionMaxRecordingTime = 30;

            //Translation
            public string TranslationPreset { get; set; } = string.Empty;
            public bool TranslationSkipLongerMessages { get; set; } = true;
            public int TranslationMaxTextLength
            {
                get { return _translationMaxTextLength; }
                set { _translationMaxTextLength = MinMax(value, 1, 60000); }
            }
            private int _translationMaxTextLength = 2000;

            //Azure
            public string AzureRegion { get; set; } = string.Empty;
            public string AzureKey { get; set; } = string.Empty;
            public string AzureSpeechLanguage { get; set; } = string.Empty;
            public string AzureCustomEndpointSpeech { get; set; } = string.Empty;
            public string AzureCustomEndpointRecognition { get; set; } = string.Empty;
            public string AzureVoice { get; set; } = string.Empty;
            public List<string> AzurePhrases { get; set; } = new();
            public List<string> AzureRecognitionLanguages { get; set; } = new();

            //Usage
            public bool TranslateTts { get; set; } = false;
            public bool TranslateTextbox { get; set; } = false;
            public bool TranslationAllowExternal { get; set; } = false;
            public bool AddOriginalAfterTranslate { get; set; } = false;
            public bool UseAzureTts { get; set; } = false;

            public int GetIndex(string name)
            {
                for (int i = 0; i < Presets.Count; i++)
                {
                    if (Presets[i].Name == name)
                        return i;
                }
                return -1;
            }

            public ApiPresetModel? GetPreset(string name)
            {
                var presetIndex = GetIndex(name);

                if (presetIndex == -1 || presetIndex >= Presets.Count)
                    return null;

                return Presets[presetIndex];
            }
        }

        /// <summary>
        /// Model for all Logging related data, this can currently only be changed in the file
        /// </summary>
        public class ConfigLoggerModel
        {
            public bool OpenLogWindow { get; set; } = false;
            public bool CheckUpdates { get; set; } = true;
            public bool Error { get; set; } = true;
            public bool Warning { get; set; } = true;
            public bool Info { get; set; } = true;
            public bool PrioInfo { get; set; } = true;
            public bool Log { get; set; } = true;
            public bool Debug { get; set; } = false;
            public List<string> LogFilter { get; set; } = new();
        }

        /// <summary>
        /// Model for all Input related data (Presets, Sending Options)
        /// </summary>
        public class ConfigInputModel
        {
            public bool UseTts { get; set; } = false;
            public bool UseTextbox { get; set; } = true;
            public bool TriggerCommands { get; set; } = true;
            public bool TriggerReplace { get; set; } = true;
            public bool IgnoreCaps { get;set; } = true;
            public bool AllowTranslation { get; set; } = true;
            public Dictionary<string, string> Presets { get; set; } = new();
        }
        #endregion

        #region Extra Models
        /// <summary>
        /// Model for storing Routing Filter data
        /// </summary>
        public class OscRoutingFilterModel
        {
            public string Name { get; set; } = "New Filter";
            public int Port
            {
                get { return _port; }
                set { _port = MinMax(value, -1, 65535); }
            }
            private int _port = -1;
            public string Ip { get; set; } = "127.0.0.1";
            public List<string> Filters { get; set; } = new();

            private bool _isValid = true;
            public override string ToString()
            => $"{(_isValid ? "" : "[x]")}{Name} => {Ip}:{Port}";

            /// <summary>
            /// Sets validity to be displayed in filter window
            /// </summary>
            public void SetValidity(bool state)
                => _isValid = state;
        }

        public class ReplacementModel
        {
            public string Text
            {
                get { return _text; }
                set {
                    _text = string.IsNullOrWhiteSpace(value) ? "New Value" : value;
                    _textLc = _text.ToLower();
                }
            }
            private string _text = "New Value";
            private string _textLc = "new value";
            public string GetTextLc() => _textLc;

            public string Replacement { get; set; } = string.Empty;
            public bool Enabled { get; set; } = true;

            public ReplacementModel(string text, string replacement, bool enabled = true)
            {
                Text = text;
                Replacement = replacement;
                Enabled = enabled;
            }
            public ReplacementModel() { }

            public override string ToString()
                => $"{(Enabled ? "" : "[x] ")}{Text} => {Replacement}";
        }

        public class ApiPresetModel
        {
            public string Name { get; set; } = "Example Preset";
            public string PostUrl { get; set; } = "https://example.net";
            public string JsonData { get; set; } = @"{""data"" : ""[T]""}";
            public Dictionary<string, string> HeaderValues { get; set; } = new();
            public string ContentType { get; set; } = string.Empty;
            public string ResultField { get; set; } = "result";
            public int ConnectionTimeout
            {
                get { return _connectionTimeout; }
                set { _connectionTimeout = MinMax(value, 25, 60000); }
            }
            private int _connectionTimeout = 3000;

            public bool IsValid()
                => !string.IsNullOrWhiteSpace(PostUrl)
                && !string.IsNullOrWhiteSpace(JsonData)
                && !string.IsNullOrWhiteSpace(ResultField);
        }
        #endregion
    }
}
