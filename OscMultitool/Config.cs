using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        public static string ModelPath { get; private set; }

        #region Saving and Loading
        static Config()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();

            ResourcePath = Path.GetFullPath(Path.Combine(assemblyDirectory, "config"));
            ConfigPath = Path.GetFullPath(Path.Combine(ResourcePath, "config.json"));
            LogPath = Path.GetFullPath(Path.Combine(ResourcePath, "log.txt"));
            ModelPath = Path.GetFullPath(Path.Combine(ResourcePath, "models"));

            try
            {
                if (!Directory.Exists(ResourcePath))
                    Directory.CreateDirectory(ResourcePath);

                if (!Directory.Exists(ModelPath))
                    Directory.CreateDirectory(ModelPath);

                string configData = File.ReadAllText(ConfigPath, Encoding.UTF8);
                Data = JsonConvert.DeserializeObject<ConfigModel>(configData) ?? new();
                TryLoadFolderModels();
            }
            catch
            {
                Data = new();
            }

            UpdateConfig(Data);

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
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Data ?? new(), Formatting.Indented), Encoding.UTF8);
                Logger.Info("Saved config file at " + ConfigPath);
            }
            catch (Exception e)
            {
                Logger.Error(e, "The config file was unable to be saved.", notify:false);
            }
        }
        #endregion

        #region Utility
        /// <summary>
        /// A combination of floor and ceil for comparables
        /// </summary>
        /// <typeparam name="T">Type to compare</typeparam>
        /// <param name="value">Value to compare</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Value, if within bounds. Min, if value smaller than min. Max, if value larger than max. If max is smaller than min, min has priority</returns>
        public static T MinMax<T>(T value, T min, T max) where T : IComparable
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        public static string GetVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            return "v." + (assembly != null ? FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion : "???");
        }

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

                if (config.Debug.LogFilter.Count == 0)
                {
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
                }

                if (config.Speech.Replacements.Count == 0)
                {
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
            
            config.ConfigVersion = 2;
        }

        /// <summary>
        /// Tries loading in models from the model folder
        /// </summary>
        private static void TryLoadFolderModels()
        {
            var foldersNames = Directory.GetDirectories(ModelPath);

            foreach (var folderName in foldersNames)
            {
                var contentFolder = GetActualModelFolder(folderName);

                if (string.IsNullOrWhiteSpace(contentFolder))
                    continue;

                var folderNameSplit = folderName.Split("\\")[^1];
                if (string.IsNullOrWhiteSpace(folderNameSplit))
                    continue;

                Speech.VoskModels[folderNameSplit] = contentFolder;
            }
        }

        /// <summary>
        /// Recursive function to return the folder that is likely holding the main model (More than 1 inner folder)
        /// </summary>
        /// <param name="folderName">Path of folder to search</param>
        /// <returns>Innermost folder</returns>
        private static string GetActualModelFolder(string folderName)
        {
            var subDirs = Directory.GetDirectories(folderName);
            var countSub = subDirs.Length;

            if (countSub == 0)
                return string.Empty;
            else if (countSub == 1)
                return GetActualModelFolder(subDirs[0]);
            else
                return folderName;
        }

        #endregion

        #region Models
        /// <summary>
        /// Model for storing all config data
        /// </summary>
        public class ConfigModel
        {
            public int ConfigVersion { get; set; } = 0;

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
            //Routing
            public string Ip { get; set; } = "127.0.0.1";
            public int Port
            {
                get { return _port; }
                set { _port = MinMax(value, -1, 65535); }
            }
            private int _port = 9000;
            public int PortListen
            {
                get { return _portListen; }
                set { _portListen = MinMax(value, -1, 65535); }
            }
            private int _portListen = 9001;
            public List<OscRoutingFilterModel> RoutingFilters { get; set; } = new();

            //Addresses
            public string AddressManualMute { get; set; } =         "/avatar/parameters/ToolMute";
            public string AddressManualSkipSpeech { get; set; } =   "/avatar/parameters/ToolSkipSpeech";
            public string AddressManualSkipBox { get; set; } =      "/avatar/parameters/ToolSkipBox";
            public string AddressEnableReplacements { get; set; } = "/avatar/parameters/ToolEnableReplacements";
            public string AddressEnableTextbox { get; set; } =      "/avatar/parameters/ToolEnableBox";
            public string AddressEnableTts { get; set; } =          "/avatar/parameters/ToolEnableTts";
            public string AddressEnableAutoMute { get; set; } =     "/avatar/parameters/ToolEnableAutoMute";
            public string AddressListeningIndicator { get; set; } = "/avatar/parameters/MicListening";
            public string AddressGameMute { get; set; } =           "/avatar/parameters/MuteSelf";
            public string AddressGameAfk { get; set; } =            "/avatar/parameters/AFK";
            public string AddressAddTextbox { get; set; } =         "/hoscy/message";
            public string AddressAddTts { get; set; } =             "/hoscy/tts";
            public string AddressAddNotification { get; set; } =    "/hoscy/notification";
            public string AddressMediaPause { get; set; } =         "/avatar/parameters/MediaPause";
            public string AddressMediaUnpause { get; set; } =       "/avatar/parameters/MediaUnpause";
            public string AddressMediaRewind { get; set; } =        "/avatar/parameters/MediaRewind";
            public string AddressMediaSkip { get; set; } =          "/avatar/parameters/MediaSkip";
            public string AddressMediaInfo { get; set; } =          "/avatar/parameters/MediaInfo";
            public string AddressMediaToggle { get; set; } =        "/avatar/parameters/MediaToggle";

            //Counters
            public bool ShowCounterNotifications { get; set; } = false;
            public float CounterDisplayDuration
            {
                get { return _counterDisplayDuration; }
                set { _counterDisplayDuration = MinMax(value, 0.01f, 30); }
            }
            private float _counterDisplayDuration = 10f;

            public List<CounterModel> Counters { get; set; } = new();

            //AFK
            public bool ShowAfkDuration { get; set; } = false;
            public float AfkDuration
            {
                get { return _afkDuration; }
                set { _afkDuration = MinMax(value, 5, 300); }
            }
            private float _afkDuration = 15;
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
            private int _voskTimeout = 2000;

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
            //Timeout, Maxlen
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

            //Notification
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
            public string AzureVoiceCurrent { get; set; } = string.Empty;
            public List<string> AzurePhrases { get; set; } = new();
            public List<string> AzureRecognitionLanguages { get; set; } = new();
            public Dictionary<string, string> AzureVoices { get; set; } = new();

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
                    _escapeText = Regex.Escape(_text);
                }
            }
            private string _text = "New Value";
            private string _escapeText = "New Value";

            public string Replacement { get; set; } = string.Empty;
            public bool Enabled { get; set; } = true;

            public ReplacementModel(string text, string replacement, bool enabled = true)
            {
                Text = text;
                Replacement = replacement;
                Enabled = enabled;
            }
            public ReplacementModel() { }

            public string EscapedText() => _escapeText;

            public override string ToString()
                => $"{(Enabled ? "" : "[x] ")}{Text} => {Replacement}";
        }

        public class ApiPresetModel
        {
            public string Name { get; set; } = "Example Preset";
            public string SentData { get; set; } = @"{""data"" : ""[T]""}";
            public Dictionary<string, string> HeaderValues { get; set; } = new();
            public string ContentType { get; set; } = "application/json";
            public string ResultField { get; set; } = "result";

            public string TargetUrl
            {
                get { return _targetUrl; }
                set
                {
                    _targetUrl = value;
                    _fullTargetUrl = value.StartsWith("h") ? value : "https://" + value;
                }
            }
            private string _targetUrl = string.Empty;
            private string _fullTargetUrl = string.Empty;

            public string Authorization
            {
                get { return _authorization; }
                set
                {
                    _authorization = string.Empty;
                    _authenticationHeader = null;

                    if (string.IsNullOrWhiteSpace(value))
                        return;

                    try {
                        _authorization = value;
                        var authSplit = value.Split(' ');

                        if (authSplit.Length == 1)
                            _authenticationHeader = new(authSplit[0]);
                        else if (authSplit.Length > 1)
                            _authenticationHeader = new(authSplit[0], string.Join(' ', authSplit[1..]));
                    }
                    catch { }
                }
            }
            private string _authorization = string.Empty;
            private AuthenticationHeaderValue? _authenticationHeader = null;

            public int ConnectionTimeout
            {
                get { return _connectionTimeout; }
                set { _connectionTimeout = MinMax(value, 25, 60000); }
            }
            private int _connectionTimeout = 3000;

            public string FullTargetUrl() => _fullTargetUrl;
            public AuthenticationHeaderValue? AuthenticationHeader() => _authenticationHeader;

            public bool IsValid()
                => !string.IsNullOrWhiteSpace(TargetUrl)
                && !string.IsNullOrWhiteSpace(SentData)
                && !string.IsNullOrWhiteSpace(ResultField)
                && !string.IsNullOrWhiteSpace(ContentType);
        }

        public class CounterModel
        {
            public string Name { get; set; } = "Unnamed Counter";
            public uint Count { get; set; } = 0;
            public DateTime LastUsed { get; set; } = DateTime.MinValue;
            public bool Enabled { get; set; } = true;
            public float Cooldown
            {
                get { return _cooldown; }
                set { _cooldown = MinMax(value, 0, 3600); }
            }
            private float _cooldown = 0;

            public string Parameter
            {
                get { return _parameter; }
                set
                {
                    _parameter = value;
                    _fullParameter = value.StartsWith("/") ? value : "/avatar/parameters/" + value;
                }
            }
            private string _parameter = "Parameter";
            private string _fullParameter = "/avatar/parameters/Parameter";

            public void Increase()
            {
                Count++;
                LastUsed = DateTime.Now;
            }

            public string FullParameter() => _fullParameter;

            public override string ToString()
                => $"{(Enabled ? "" : "[x] ")}{Name}: {Count:N0}";
        }
        #endregion
    }
}
