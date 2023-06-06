using System.Collections.Generic;
using Whisper;

namespace Hoscy.Models
{
    /// <summary>
    /// Model for storing all config data, they can not log
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
            set => _recognitionMaxRecordingTime = Utils.MinMax(value, 1, 300);
        }
        private int _recognitionMaxRecordingTime = 30;

        //Translation
        public string TranslationPreset { get; set; } = string.Empty; //API preset for translation
        public bool TranslationSkipLongerMessages { get; set; } = true; //Skipping messages that are too long instead of partial translation
        public int TranslationMaxTextLength //Maximum length of translatable text
        {
            get => _translationMaxTextLength;
            set => _translationMaxTextLength = Utils.MinMax(value, 1, short.MaxValue);
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
    /// Model for all Input related data (Presets, Sending Options)
    /// </summary>
    internal class ConfigInputModel
    {
        public bool UseTts { get; set; } = false; //Convert input to TTS
        public bool UseTextbox { get; set; } = true; //Convert input to textbox
        public bool TriggerCommands { get; set; } = true; //Input triggers commands
        public bool TriggerReplace { get; set; } = true; //Input triggers replacements
        public bool AllowTranslation { get; set; } = true; //Translate input
        public Dictionary<string, string> Presets { get; set; } = new(); //Presets for quick access
    }

    /// <summary>
    /// Model for all Logging related data, this can currently only be changed in the file
    /// </summary>
    internal class ConfigLoggerModel
    {
        public bool OpenLogWindow { get; set; } = false; //Open log window on startup
        public bool CheckUpdates { get; set; } = true; //Automatically check for updates on startup
        public LogSeverity MinimumLogSeverity { get; set; } = LogSeverity.Log;
        public List<FilterModel> LogFilters { get; set; } = new(); //Phrases filtered from logs
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
            set => _port = Utils.MinMax(value, -1, 65535);
        }
        private int _port = 9000;
        public int PortListen //Port HOSCY listens on
        {
            get => _portListen;
            set => _portListen = Utils.MinMax(value, -1, 65535);
        }
        private int _portListen = 9001;
        public List<OscRoutingFilterModel> RoutingFilters { get; set; } = new(); //Routing

        //Addresses for OSC Control
        public string AddressManualMute { get; set; } = "/avatar/parameters/ToolMute";
        public string AddressManualSkipSpeech { get; set; } = "/avatar/parameters/ToolSkipSpeech";
        public string AddressManualSkipBox { get; set; } = "/avatar/parameters/ToolSkipBox";
        public string AddressEnableReplacements { get; set; } = "/avatar/parameters/ToolEnableReplacements";
        public string AddressEnableTextbox { get; set; } = "/avatar/parameters/ToolEnableBox";
        public string AddressEnableTts { get; set; } = "/avatar/parameters/ToolEnableTts";
        public string AddressEnableAutoMute { get; set; } = "/avatar/parameters/ToolEnableAutoMute";
        public string AddressListeningIndicator { get; set; } = "/avatar/parameters/MicListening";
        public string AddressGameMute { get; set; } = "/avatar/parameters/MuteSelf";
        public string AddressGameAfk { get; set; } = "/avatar/parameters/AFK";
        public string AddressGameTextbox { get; set; } = "/chatbox/input";
        public string AddressAddTextbox { get; set; } = "/hoscy/message";
        public string AddressAddTts { get; set; } = "/hoscy/tts";
        public string AddressAddNotification { get; set; } = "/hoscy/notification";
        public string AddressMediaPause { get; set; } = "/avatar/parameters/MediaPause";
        public string AddressMediaUnpause { get; set; } = "/avatar/parameters/MediaUnpause";
        public string AddressMediaRewind { get; set; } = "/avatar/parameters/MediaRewind";
        public string AddressMediaSkip { get; set; } = "/avatar/parameters/MediaSkip";
        public string AddressMediaInfo { get; set; } = "/avatar/parameters/MediaInfo";
        public string AddressMediaToggle { get; set; } = "/avatar/parameters/MediaToggle";

        //Counter Related
        public bool ShowCounterNotifications { get; set; } = false; //Display in Textbox
        public float CounterDisplayDuration //Duration (in seconds) that counters will be accounted for in display
        {
            get => _counterDisplayDuration;
            set => _counterDisplayDuration = Utils.MinMax(value, 0.01f, 30);
        }
        private float _counterDisplayDuration = 10f;

        public float CounterDisplayCooldown //Duration (in seconds) that counters cannot be displayed
        {
            get => _counterDisplayCooldown;
            set => _counterDisplayCooldown = Utils.MinMax(value, 0, 300);
        }
        private float _counterDisplayCooldown = 0f;

        public List<CounterModel> Counters { get; set; } = new();

        //AFK Related
        public bool ShowAfkDuration { get; set; } = false; //Display in Textbox
        public float AfkDuration //Duration (in seconds) between initial AFK notifications
        {
            get => _afkDuration;
            set => _afkDuration = Utils.MinMax(value, 5, 300);
        }
        private float _afkDuration = 15;

        public float AfkDoubleDuration //Amount of times the duration gets displayed before duration doubles
        {
            get => _afkDoubleDuration;
            set => _afkDoubleDuration = Utils.MinMax(value, 0, 60);
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
            set => _speakerVolume = Utils.MinMax(value, 0, 1);
        }
        private float _speakerVolume = 0.5f;
        public int MaxLenTtsString //Max length of strings that get TTS
        {
            get => _maxLenTtsString;
            set => _maxLenTtsString = Utils.MinMax(value, 1, short.MaxValue);
        }
        private int _maxLenTtsString = 500;
        public bool SkipLongerMessages { get; set; } = true; //Skip longer messages instead of cutting off

        //Vosk
        public Dictionary<string, string> VoskModels { get; set; } = new(); //Model identifiers and filepaths
        public string VoskModelCurrent { get; set; } = string.Empty; //Identifier for current model
        public int VoskTimeout //Time (in milliseconds) allowed to pass where no new words are identified before manually cutting off
        {
            get => _voskTimeout;
            set => _voskTimeout = Utils.MinMax(value, 500, 30000);
        }
        private int _voskTimeout = 2500;

        //Whisper
        public Dictionary<string, string> WhisperModels { get; set; } = new(); //Model identifiers and filepaths
        public string WhisperModelCurrent { get; set; } = string.Empty; //Identifier for current model

        public bool WhisperSingleSegment { get; set; } = true; //Enables single segment mode (Higher accuracy, reduced functionality)
        //public bool WhisperSpeedup { get; set; } = false; //Enables speedup (Higher speed, lower accuracy), disabled for now due to library issues
        public bool WhisperToEnglish { get; set; } = false; //Translates to english
        public bool WhisperBracketFix { get; set; } = true; //Fixes the bracket issue ('( ( (')
        public bool WhisperHighPerformance { get; set; } = false; //Enables heightened thread priority
        public bool WhisperLogFilteredNoises { get; set; } = false; //Logging of filtered noises
        public bool WhisperCpuOnly { get; set;} = false; //Enables CPU Only mode

        public eLanguage WhisperLanguage { get; set; } = eLanguage.English;

        public Dictionary<string,string> WhisperNoiseWhitelist { get; set; } = new();

        public int WhisperThreads //Threads for whisper to use, 0 = infinite
        {
            get => _whisperThreads;
            set => _whisperThreads = Utils.MinMax(value, short.MinValue, short.MaxValue);
        }
        private int _whisperThreads = -4;

        public int WhisperMaxContext //Max context for whisper to use, -1 = infinite
        {
            get => _whisperMaxContext;
            set => _whisperMaxContext = Utils.MinMax(value, -1, short.MaxValue);
        }
        private int _whisperMaxContext = 0;

        public int WhisperMaxSegLen //Max segment length for whisper, 0 = infinite
        {
            get => _whisperMaxSegLen;
            set => _whisperMaxSegLen = Utils.MinMax(value, 0, short.MaxValue);
        }
        private int _whisperMaxSegLen = 0;

        public float WhisperRecMaxDuration //Maximum duration for recognition
        {
            get => _whisperRecMaxDuration;
            set => _whisperRecMaxDuration = Utils.MinMax(value, 2, short.MaxValue);
        }
        private float _whisperRecMaxDuration = 16;

        public float WhisperRecPauseDuration //Duration for pauses
        {
            get => _whisperRecPauseDuration;
            set => _whisperRecPauseDuration = Utils.MinMax(value, 0.05f, short.MaxValue);
        }
        private float _whisperRecPauseDuration = 0.5f; //todo: [TEST] Test effect of this value

        //Windows
        public string WinModelId { get; set; } = string.Empty; //Identifier for Microsoft Recognizer

        //Replacement
        public List<string> NoiseFilter { get; set; } = new(); //List of words deemed noise
        public bool RemovePeriod { get; set; } = true; //Removes period at end
        public bool CapitalizeFirst { get; set; } = true; //Capitalizes first letter
        public bool UseReplacements { get; set; } = true; //Are replacements (and shortcuts) even used?
        public List<ReplacementDataModel> Shortcuts { get; set; } = new();
        public List<ReplacementDataModel> Replacements { get; set; } = new();
        public string ShortcutIgnoredCharacters { get; set; } = ".?!,。、！？";
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
            set => _maxLength = Utils.MinMax(value, 50, 130);
        }
        private int _maxLength = 130;
        public int TimeoutMultiplier //Add x milliseconds to timeout per 20 characters
        {
            get => _timeoutMultiplier;
            set => _timeoutMultiplier = Utils.MinMax(value, 250, 10000);
        }
        private int _timeoutMultiplier = 1250;

        public int MinimumTimeout //Minimum timeout (in milliseconds) that a message stays in the chatbox
        {
            get => _minimumTimeout;
            set => _minimumTimeout = Utils.MinMax(value, 1250, 30000);
        }
        private int _minimumTimeout = 3000;

        public int DefaultTimeout //Default timeout (in milliseconds) that a message stays in the chatbox
        {
            get => _defaultTimeout;
            set => _defaultTimeout = Utils.MinMax(value, 1250, 30000);
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
        public bool MediaSwapArtistAndSong { get; set; } = false; //Swaps order of artist and song

        public string MediaPlayingVerb //xyz "songname" by "artist" on "album"
        {
            get => _mediaPlayingVerb;
            set => _mediaPlayingVerb = value.Length > 0 ? value : "Playing";
        }
        private string _mediaPlayingVerb = "Playing";

        public string MediaArtistVerb //Playing "songname" xyz "artist" on "album"
        {
            get => _mediaArtistVerb;
            set => _mediaArtistVerb = value.Length > 0 ? value : "by";
        }
        private string _mediaArtistVerb = "by";

        public string MediaAlbumVerb //Playing "songname" by "artist" xyz "album"
        {
            get => _mediaAlbumVerb;
            set => _mediaAlbumVerb = value.Length > 0 ? value : "on";
        }
        private string _mediaAlbumVerb = "on";

        public string MediaExtra { get; set; } = string.Empty;

        public List<FilterModel> MediaFilters { get; set; } = new(); //Phrases filtered from media

        private int CalcNotificationIndicatorLength()
            => _notificationIndicatorRight.Length + _notificationIndicatorLeft.Length;

        internal int NotificationIndicatorLength() => _notificationIndicatorLength;
    }
}
