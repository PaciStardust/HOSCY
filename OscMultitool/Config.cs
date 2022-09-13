using Newtonsoft.Json;
using OscMultitool.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace OscMultitool
{
    public static class Config
    {
        public static ConfigModel Data { get; private set; }
        public static ConfigOscModel Osc => Data.Osc;
        public static ConfigSpeechModel Speech => Data.Speech;
        public static ConfigTextboxModel Textbox => Data.Textbox;
        public static ConfigInputModel Input => Data.Input;
        public static ConfigApiModel Api => Data.Api;
        public static ConfigLoggerModel Logging => Data.Logging;

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
                Data = new();
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
                Logger.PInfo("Saved config file at " + ConfigPath, "Config");
            }
            catch (Exception e)
            {
                Logger.Error(e, "ConfigSave", false);
            }
        }
        #endregion

        #region Utility
        public static int MinMax(int value, int min, int max)
            => Math.Max(Math.Min(max, value), min);
        public static float MinMax(float value, float min, float max)
            => Math.Max(Math.Min(max, value), min);
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
            public ConfigLoggerModel Logging { get; init; } = new();
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
            public string AddressListeningIndicator { get; set; } = "/avatar/parameters/MicListening";
            public string AddressAddTextbox { get; set; } = "/hoscy/textbox";
            public string AddressAddTts { get; set; } = "/hoscy/tts";
            public string AddressAddNotification { get; set; } = "/hoscy/notification";
            public List<ConfigOscRoutingFilterModel> RoutingFilters { get; set; } = new();
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
            public bool TranslateTts { get; set; } = false;

            //Vosk
            public string VoskModelPath { get; set; } = string.Empty;
            public float VoskTimeout
            {
                get { return _voskTimeout; }
                set { _voskTimeout = MinMax(value, 0.5f, 30); }
            }
            private float _voskTimeout = 3f;

            //Windows
            public string WinModelId { get; set; } = string.Empty;

            //Replacement
            public List<string> NoiseFilter { get; set; } = new();
            public bool IgnoreCaps { get; set; } = true;
            public string MediaControlKeyword { get; set; } = "media";
            public Dictionary<string, string> Shortcuts { get; set; } = new();
            public Dictionary<string, string> Replacements { get; set; } = new();
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
                set { _timeoutMultiplier = MinMax(value, 1000, 10000); }
            }
            private int _timeoutMultiplier = 1500;
            public int DefaultTimeout
            {
                get { return _defaultTimeout; }
                set { _defaultTimeout = MinMax(value, 1000, 30000); }
            }
            private int _defaultTimeout = 5000;
            public bool DynamicTimeout { get; set; } = true;
            public bool TranslateTextbox { get; set; } = false;
            public bool AddOriginalAfterTranslate { get; set; } = false;
            public bool ShowMediaStatus { get; set; } = false;
            public bool AutomaticClearNotification { get; set; } = true;
            public bool AutomaticClearMessage { get; set; } = false;
        }

        /// <summary>
        /// Model for all API related data
        /// </summary>
        public class ConfigApiModel
        {
            //General
            public List<ConfigApiPresetModel> Presets { get; set; } = new();

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
            public bool TranslationAllowExternal { get; set; } = false;
            public bool TranslationSkipLongerMessages { get; set; } = true;
            public int TranslationMaxTextLength
            {
                get { return _translationMaxTextLength; }
                set { _translationMaxTextLength = MinMax(value, 1, 60000); }
            }
            private int _translationMaxTextLength = 2000;

            //Azure Rec
            public string AzureRegion { get; set; } = string.Empty;
            public string AzureKey { get; set; } = string.Empty;
            public bool AzureRemoveFullStop { get; set; } = true;
            public string AzureLanguage { get; set; } = string.Empty;
            public string AzureCustomEndpoint { get; set; } = string.Empty;

            public int GetIndex(string name)
            {
                for (int i = 0; i < Presets.Count; i++)
                {
                    if (Presets[i].Name == name)
                        return i;
                }
                return -1;
            }

            public ConfigApiPresetModel? GetPreset(string name)
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
        public class ConfigOscRoutingFilterModel
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

            public override string ToString()
            => $"{Name} => {Ip}:{Port}";
        }

        public class ConfigApiPresetModel
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
