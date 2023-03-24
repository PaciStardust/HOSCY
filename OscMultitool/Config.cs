using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
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
        internal static void SaveConfig(bool backup = false)
        {
            try
            {
                var jsonText = JsonConvert.SerializeObject(Data ?? new(), Formatting.Indented);
                File.WriteAllText(Utils.PathConfigFile, jsonText, Encoding.UTF8);
                if (backup)
                    File.WriteAllText(Utils.PathConfigFile + ".backup", jsonText, Encoding.UTF8);
                Logger.Info($"Saved config file {(backup ? "and backup " : string.Empty)}at " + Utils.PathConfigFile);
            }
            catch (Exception e)
            {
                Logger.Error(e, "The config file was unable to be saved.", notify:false);
            }
        }

        /// <summary>
        /// Tries loading in models from the model folder
        /// </summary>
        private static void TryLoadFolderModels()
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
        /// A combination of floor and ceil for comparables
        /// </summary>
        /// <typeparam name="T">Type to compare</typeparam>
        /// <param name="value">Value to compare</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Value, if within bounds. Min, if value smaller than min. Max, if value larger than max. If max is smaller than min, min has priority</returns>
        internal static T MinMax<T>(T value, T min, T max) where T : IComparable
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
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

        #region Models
        /// <summary>
        /// Model for storing all config data
        /// </summary>
        internal class ConfigModel
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
        internal class ConfigOscModel
        {
            //Routing Related
            public string Ip { get; set; } = "127.0.0.1"; //Target IP for sending
            public int Port //Target port for sending
            {
                get => _port;
                set { _port = MinMax(value, -1, 65535); }
            }
            private int _port = 9000;
            public int PortListen //Port HOSCY listens on
            {
                get => _portListen;
                set { _portListen = MinMax(value, -1, 65535); }
            }
            private int _portListen = 9001;
            public List<OscRoutingFilterModel> RoutingFilters { get; set; } = new(); //Routing

            //Addresses for OSC Control
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
            public string AddressGameTextbox { get; set; } =        "/chatbox/input";
            public string AddressAddTextbox { get; set; } =         "/hoscy/message";
            public string AddressAddTts { get; set; } =             "/hoscy/tts";
            public string AddressAddNotification { get; set; } =    "/hoscy/notification";
            public string AddressMediaPause { get; set; } =         "/avatar/parameters/MediaPause";
            public string AddressMediaUnpause { get; set; } =       "/avatar/parameters/MediaUnpause";
            public string AddressMediaRewind { get; set; } =        "/avatar/parameters/MediaRewind";
            public string AddressMediaSkip { get; set; } =          "/avatar/parameters/MediaSkip";
            public string AddressMediaInfo { get; set; } =          "/avatar/parameters/MediaInfo";
            public string AddressMediaToggle { get; set; } =        "/avatar/parameters/MediaToggle";

            //Counter Related
            public bool ShowCounterNotifications { get; set; } = false; //Display in Textbox
            public float CounterDisplayDuration //Duration (in seconds) that counters will be accounted for in display
            {
                get => _counterDisplayDuration;
                set { _counterDisplayDuration = MinMax(value, 0.01f, 30); }
            }
            private float _counterDisplayDuration = 10f;

            public float CounterDisplayCooldown //Duration (in seconds) that counters cannot be displayed
            {
                get => _counterDisplayCooldown;
                set { _counterDisplayCooldown = MinMax(value, 0, 300); }
            }
            private float _counterDisplayCooldown = 0f;

            public List<CounterModel> Counters { get; set; } = new();

            //AFK Related
            public bool ShowAfkDuration { get; set; } = false; //Display in Textbox
            public float AfkDuration //Duration (in seconds) between initial AFK notifications
            {
                get => _afkDuration;
                set { _afkDuration = MinMax(value, 5, 300); }
            }
            private float _afkDuration = 15;

            public float AfkDoubleDuration //Amount of times the duration gets displayed before duration doubles
            {
                get => _afkDoubleDuration;
                set { _afkDoubleDuration = MinMax(value, 0, 60); }
            }
            private float _afkDoubleDuration = 12;
        }

        /// <summary>
        /// Model for storing all Speech related data (TTS, Models, Device Options)
        /// </summary>
        internal class ConfigSpeechModel
        {
            //Usage
            public bool UseTextbox { get; set; } = true; //Result will be sent through textbox
            public bool UseTts { get; set; } = false; //Result will be sent through TTS
            public string MicId { get; set; } = string.Empty; //Recording microphone
            public bool StartUnmuted { get; set; } = true; //Start recognition unmuted
            public bool PlayMuteSound { get; set; } = false; //Play mute and unmute sounds
            public bool MuteOnVrcMute { get; set; } = true; //Automatically mute when muted in VRC
            public string ModelName { get; set; } = string.Empty; //Name of used model

            //TTS
            public string TtsId { get; set; } = string.Empty; //Identifier for Microsoft TTS
            public string SpeakerId { get; set; } = string.Empty; //Speaker for TTS out
            public float SpeakerVolume //TTS volume
            {
                get => _speakerVolume;
                set { _speakerVolume = MinMax(value, 0, 1); }
            }
            private float _speakerVolume = 0.5f;
            public int MaxLenTtsString //Max length of strings that get TTS
            {
                get => _maxLenTtsString;
                set { _maxLenTtsString = MinMax(value, 1, 99999); }
            }
            private int _maxLenTtsString = 500;
            public bool SkipLongerMessages { get; set; } = true; //Skip longer messages instead of cutting off

            //Vosk
            public Dictionary<string, string> VoskModels { get; set; } = new(); //Model identifiers and filepaths
            public string VoskModelCurrent { get; set; } = string.Empty; //Identifier for current model
            public int VoskTimeout //Time (in milliseconds) allowed to pass where no new words are identified before manually cutting off
            {
                get => _voskTimeout;
                set { _voskTimeout = MinMax(value, 500, 30000); }
            }
            private int _voskTimeout = 2500;

            //Windows
            public string WinModelId { get; set; } = string.Empty; //Identifier for Microsoft Recognizer

            //Replacement
            public List<string> NoiseFilter { get; set; } = new(); //List of words deemed noise
            public bool IgnoreCaps { get; set; } = true; //Ignoring caps when testing for replacements
            public bool RemoveFullStop { get; set; } = true; //Removing the full stop at the end of a sentence if available
            public bool UseReplacements { get; set; } = true; //Are replacements (and shortcuts) even used?
            public List<ReplacementModel> Shortcuts { get; set; } = new();
            public List<ReplacementModel> Replacements { get; set; } = new();
        }

        /// <summary>
        /// Model for storing all Textbox related data (Timeouts, MaxLength)
        /// </summary>
        internal class ConfigTextboxModel
        {
            //Timeout, Maxlen
            public int MaxLength //Max length of string displayed before cutoff
            {
                get => _maxLength;
                set { _maxLength = MinMax(value, 50, 130); }
            }
            private int _maxLength = 130;
            public int TimeoutMultiplier //Add x milliseconds to timeout per 20 characters
            {
                get => _timeoutMultiplier;
                set { _timeoutMultiplier = MinMax(value, 250, 10000); }
            }
            private int _timeoutMultiplier = 1250;

            public int MinimumTimeout //Minimum timeout (in milliseconds) that a message stays in the chatbox
            {
                get => _minimumTimeout;
                set { _minimumTimeout = MinMax(value, 1250, 30000); }
            }
            private int _minimumTimeout = 3000;

            public int DefaultTimeout //Default timeout (in milliseconds) that a message stays in the chatbox
            {
                get => _defaultTimeout;
                set { _defaultTimeout = MinMax(value, 1250, 30000); }
            }
            private int _defaultTimeout = 5000;
            public bool DynamicTimeout { get; set; } = true; //Enables the dynamic timeout

            //Visual
            public bool AutomaticClearNotification { get; set; } = true; //Automatic clearing after notification timeout
            public bool AutomaticClearMessage { get; set; } = false; //Automatic clearing after message timeout
            public bool UseIndicatorWithoutBox { get; set; } = false; //"Typing" indicator when box is disabled

            public string NotificationIndicatorLeft //Text to the left of a notification
            {
                get => _notificationIndicatorLeft;
                set
                {
                    _notificationIndicatorLeft = value.Length < 4 ? value : value[..3];
                    _notificationIndicatorLength = CalcNotificationIndicatorLength();
                }
            }
            private string _notificationIndicatorLeft = "〈";

            public string NotificationIndicatorRight //Text to the right of a notification
            {
                get => _notificationIndicatorRight;
                set
                {
                    _notificationIndicatorRight = value.Length < 4 ? value : value[..3];
                    _notificationIndicatorLength = CalcNotificationIndicatorLength();
                }
            }
            private string _notificationIndicatorRight = "〉";
            private int _notificationIndicatorLength = 2;

            //Sound
            public bool SoundOnMessage { get; set; } = true; //Play sound on textbox message
            public bool SoundOnNotification { get; set; } = false; //Play sound on textbox notification

            //Notification handling
            public bool UseNotificationPriority { get; set; } = true; //Only allows overwriting notification current not of lower priority
            public bool UseNotificationSkip { get; set; } = true; //Skip notifications if a message is available

            //Media
            public bool MediaShowStatus { get; set; } = false; //Display media information in textbox
            public bool MediaAddAlbum { get; set; } = false; //Also add album to media information

            public string MediaPlayingVerb //xyz "songname" by "artist" on "album"
            {
                get => _mediaPlayingVerb;
                set { _mediaPlayingVerb = value.Length > 0 ? value : "Playing"; }
            }
            private string _mediaPlayingVerb = "Playing";

            public string MediaArtistVerb //Playing "songname" xyz "artist" on "album"
            {
                get => _mediaArtistVerb;
                set { _mediaArtistVerb = value.Length > 0 ? value : "by"; }
            }
            private string _mediaArtistVerb = "by";

            public string MediaAlbumVerb //Playing "songname" by "artist" xyz "album"
            {
                get => _mediaAlbumVerb;
                set { _mediaAlbumVerb = value.Length > 0 ? value : "on"; }
            }
            private string _mediaAlbumVerb = "on";

            public string MediaExtra { get; set; } = string.Empty;

            public List<FilterModel> MediaFilters { get; set; } = new(); //Phrases filtered from media

            private int CalcNotificationIndicatorLength()
                => _notificationIndicatorRight.Length + _notificationIndicatorLeft.Length;

            internal int NotificationIndicatorLength() => _notificationIndicatorLength;
        }

        /// <summary>
        /// Model for all API related data
        /// </summary>
        internal class ConfigApiModel
        {
            //General
            public List<ApiPresetModel> Presets { get; set; } = new(); 

            //Recognition
            public string RecognitionPreset { get; set; } = string.Empty; //API preset for recognition
            public int RecognitionMaxRecordingTime //Max time (in seconds) that can be recorded at once
            {
                get => _recognitionMaxRecordingTime;
                set { _recognitionMaxRecordingTime = MinMax(value, 1, 300); }
            }
            private int _recognitionMaxRecordingTime = 30;

            //Translation
            public string TranslationPreset { get; set; } = string.Empty; //API preset for translation
            public bool TranslationSkipLongerMessages { get; set; } = true; //Skipping messages that are too long instead of partial translation
            public int TranslationMaxTextLength //Maximum length of translatable text
            {
                get => _translationMaxTextLength;
                set { _translationMaxTextLength = MinMax(value, 1, 60000); }
            }
            private int _translationMaxTextLength = 2000;

            //Azure
            public string AzureRegion { get; set; } = string.Empty; //Region for azure
            public string AzureKey { get; set; } = string.Empty; //Cognitive services key
            public string AzureSpeechLanguage { get; set; } = string.Empty; //Language of TTS
            public string AzureCustomEndpointSpeech { get; set; } = string.Empty; //Custom speech endpoint
            public string AzureCustomEndpointRecognition { get; set; } = string.Empty; //Custom recognition endpoint
            public string AzureVoiceCurrent { get; set; } = string.Empty; //Current azure voice
            public List<string> AzurePhrases { get; set; } = new(); //Phrases to set for improved recognition
            public List<string> AzureRecognitionLanguages { get; set; } = new(); //All voices for speech recognition
            public Dictionary<string, string> AzureVoices { get; set; } = new(); //All voices for TTS

            //Usage
            public bool TranslateTts { get; set; } = false; //Automatically translate TTS
            public bool TranslateTextbox { get; set; } = false; //Automatically translate textbox
            public bool TranslationAllowExternal { get; set; } = false; //Translate external
            public bool AddOriginalAfterTranslate { get; set; } = false; //Add original version after translation
            public bool UseAzureTts { get; set; } = false; //Use azure TTS instead of Microsoft

            internal int GetIndex(string name)
            {
                for (int i = 0; i < Presets.Count; i++)
                {
                    if (Presets[i].Name == name)
                        return i;
                }
                return -1;
            }

            internal ApiPresetModel? GetPreset(string name)
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
        internal class ConfigLoggerModel
        {
            public bool OpenLogWindow { get; set; } = false; //Open log window on startup
            public bool CheckUpdates { get; set; } = true; //Automatically check for updates on startup
            public bool Error { get; set; } = true;
            public bool Warning { get; set; } = true;
            public bool Info { get; set; } = true;
            public bool PrioInfo { get; set; } = true;
            public bool Log { get; set; } = true;
            public bool Debug { get; set; } = false;
            public List<FilterModel> LogFilters { get; set; } = new(); //Phrases filtered from logs
        }

        /// <summary>
        /// Model for all Input related data (Presets, Sending Options)
        /// </summary>
        internal class ConfigInputModel
        {
            public bool UseTts { get; set; } = false; //Convert input to TTS
            public bool UseTextbox { get; set; } = true; //Convert input to textbox
            public bool TriggerCommands { get; set; } = true; //Input triggers commands
            public bool TriggerReplace { get; set; } = true; //Input triggers replacements
            public bool IgnoreCaps { get;set; } = true; //Ignore caps for commands and replacements
            public bool AllowTranslation { get; set; } = true; //Translate input
            public Dictionary<string, string> Presets { get; set; } = new(); //Presets for quick access
        }
        #endregion

        #region Extra Models
        /// <summary>
        /// Model for storing Routing Filter data
        /// </summary>
        internal class OscRoutingFilterModel
        {
            public string Name
            {
                get => _name;
                set { _name = string.IsNullOrWhiteSpace(value) ? "Unnamed Filter" : value; }
            }
            private string _name = "Unnamed Filter";
            public int Port
            {
                get => _port;
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
            internal void SetValidity(bool state)
                => _isValid = state;
        }

        internal class ReplacementModel
        {
            public string Text
            {
                get => _text;
                set {
                    _text = string.IsNullOrWhiteSpace(value) ? "New Value" : value;
                    _regexPattern = $@"(?<= |\b){Regex.Escape(_text)}(?=$| |\b)";
                    _lowercaseText = _text.ToLower();
                }
            }
            private string _text = "New Value";
            private string _regexPattern = $@"(?<= |\b)New\ Value(?= |\b)"; //Pattern for replacing text when used as replacement
            private string _lowercaseText = "new value"; //Lowercase version when checking for shortcuts

            public string Replacement { get; set; } = "Example";
            public bool Enabled { get; set; } = true;

            public ReplacementModel(string text, string replacement, bool enabled = true)
            {
                Text = text;
                Replacement = replacement;
                Enabled = enabled;
            }
            public ReplacementModel() { }

            internal string RegexPattern() => _regexPattern;
            internal string LowercaseText() => _lowercaseText;

            public override string ToString()
                => $"{(Enabled ? "" : "[x] ")}{Text} => {Replacement}";
        }

        internal class ApiPresetModel
        {
            public string Name
            {
                get => _name;
                set { _name = string.IsNullOrWhiteSpace(value) ? "Unnamed Preset" : value; }
            }
            private string _name = "Unnamed Preset";
            public string SentData { get; set; } = @"{""data"" : ""[T]""}";
            public Dictionary<string, string> HeaderValues { get; set; } = new();
            public string ContentType { get; set; } = "application/json";
            public string ResultField { get; set; } = "result";

            public string TargetUrl
            {
                get => _targetUrl;
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
                get => _authorization;
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
                get => _connectionTimeout;
                set { _connectionTimeout = MinMax(value, 25, 60000); }
            }
            private int _connectionTimeout = 3000;

            internal string FullTargetUrl() => _fullTargetUrl;
            internal AuthenticationHeaderValue? AuthenticationHeader() => _authenticationHeader;

            internal bool IsValid()
                => !string.IsNullOrWhiteSpace(TargetUrl)
                && !string.IsNullOrWhiteSpace(SentData)
                && !string.IsNullOrWhiteSpace(ResultField)
                && !string.IsNullOrWhiteSpace(ContentType);
        }

        internal class CounterModel
        {
            public string Name
            {
                get => _name;
                set { _name = string.IsNullOrWhiteSpace(value) ? "Unnamed Counter" : value; }
            }
            private string _name = "Unnamed Counter";
            public uint Count { get; set; } = 0;
            public DateTime LastUsed { get; set; } = DateTime.MinValue;
            public bool Enabled { get; set; } = true;
            public float Cooldown
            {
                get => _cooldown;
                set { _cooldown = MinMax(value, 0, 3600); }
            }
            private float _cooldown = 0;

            public string Parameter
            {
                get => _parameter;
                set
                {
                    _parameter = value;
                    _fullParameter = value.StartsWith("/") ? value : "/avatar/parameters/" + value;
                }
            }
            private string _parameter = "Parameter";
            private string _fullParameter = "/avatar/parameters/Parameter";

            internal void Increase()
            {
                Count++;
                LastUsed = DateTime.Now;
            }

            internal string FullParameter() => _fullParameter;

            public override string ToString()
                => $"{(Enabled ? "" : "[x] ")}{Name}: {Count:N0}";
        }

        internal class FilterModel
        {
            public string Name
            {
                get => _name;
                set { _name = string.IsNullOrWhiteSpace(value) ? "Unnamed Filter" : value; }
            }
            private string _name = "Unnamed Filter";
            public string FilterString
            {
                get => _filterString;
                set
                {
                    _filterString = string.IsNullOrWhiteSpace(value) ? "Filter Text" : value;
                    UpdateRegex();
                }
            }
            private string _filterString = "Filter Text";
            public bool Enabled { get; set; } = true;
            public bool IgnoreCase
            {
                get => _ignoreCase;
                set
                {
                    _ignoreCase = value;
                    UpdateRegex();
                }
            }
            private bool _ignoreCase = false;
            public bool UseRegex
            {
                get => _useRegex;
                set
                {
                    _useRegex = value;
                    UpdateRegex();
                }
            }
            private bool _useRegex = false;
            private Regex? _filterRegex;

            public FilterModel(string name, string filterString)
            {
                Name = name;
                FilterString = filterString;
            }
            public FilterModel() { }

            private void UpdateRegex()
                => _filterRegex = _useRegex ? new(_filterString, _ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None | RegexOptions.CultureInvariant) : null;

            internal bool Matches(string compare)
            {
                if (!Enabled)
                    return false;

                if (_filterRegex == null)
                    return compare.Contains(_filterString, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

                return _filterRegex.IsMatch(compare);
            }

            public override string ToString()
                => $"{(Enabled ? "" : "[x] ")}{_name} ={(_useRegex ? "R" : string.Empty)}{(_ignoreCase ? "C" : string.Empty)}> {_filterString}";
        }
        #endregion
    }
}
