using System;
using System.Collections.Generic;
using Whisper;

namespace Hoscy.Configuration.Legacy;

internal class LegacyConfigModel
{
    public int ConfigVersion { get; set; } = 0;

    public LegacyConfigOscModel Osc { get; init; } = new();
    public LegacyConfigSpeechModel Speech { get; init; } = new();
    public LegacyConfigTextboxModel Textbox { get; init; } = new();
    public LegacyConfigInputModel Input { get; init; } = new();
    public LegacyConfigApiModel Api { get; init; } = new();
    public LegacyConfigLoggerModel Debug { get; init; } = new();
}

internal class LegacyConfigApiModel
{
    public List<LegacyApiPresetModel> Presets { get; set; } = [];
    public string RecognitionPreset { get; set; } = string.Empty;
    public int RecognitionMaxRecordingTime { get; set; } = 30;
    public string TranslationPreset { get; set; } = string.Empty;
    public bool TranslationSkipLongerMessages { get; set; } = true;
    public int TranslationMaxTextLength { get; set; } = 2000;
    public string AzureRegion { get; set; } = string.Empty; 
    public string AzureKey { get; set; } = string.Empty;
    public string AzureCustomEndpointSpeech { get; set; } = string.Empty; 
    public string AzureCustomEndpointRecognition { get; set; } = string.Empty; 
    public string AzureTtsVoiceCurrent { get; set; } = string.Empty; 
    public List<string> AzurePhrases { get; set; } = [];
    public List<string> AzureRecognitionLanguages { get; set; } = []; 
    public List<LegacyAzureTtsVoiceModel> AzureTtsVoices { get; set; } = []; 
    public bool TranslateTts { get; set; }
    public bool TranslateTextbox { get; set; }
    public bool TranslationAllowExternal { get; set; }
    public bool AddOriginalAfterTranslate { get; set; }
    public bool UseAzureTts { get; set; }
}

internal class LegacyConfigInputModel
{
    public bool UseTts { get; set; }
    public bool UseTextbox { get; set; }
    public bool TriggerCommands { get; set; } = true; 
    public bool TriggerReplace { get; set; } = true; 
    public bool AllowTranslation { get; set; } = true;
    public Dictionary<string, string> Presets { get; set; } = [];
}

internal class LegacyConfigLoggerModel
{
    public bool OpenLogWindow { get; set; }
    public bool CheckUpdates { get; set; } = true;
    public List<LegacyFilterModel> LogFilters { get; set; } = [];
}

internal class LegacyConfigOscModel
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9000;
    public int PortListen { get; set; } = 9001;
    public List<LegacyOscRoutingFilterModel> RoutingFilters { get; set; } = [];
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
    public bool ShowCounterNotifications { get; set; }
    public float CounterDisplayDuration { get; set; } = 10f;
    public float CounterDisplayCooldown { get; set; } = 0f;
    public List<LegacyCounterModel> Counters { get; set; } = [];
    public bool ShowAfkDuration { get; set; }
    public float AfkDuration { get; set; } = 15;
    public float AfkDoubleDuration { get; set; } = 12;
    public string AfkStartText { get; set; } = "Now AFK";
    public string AfkEndText { get; set; } = "No longer AFK";
    public string AfkStatusText { get; set; } = "AFK since";
}

internal class LegacyConfigSpeechModel
{
    public bool UseTextbox { get; set; } = true;
    public bool UseTts { get; set; }
    public string MicId { get; set; } = string.Empty;
    public bool StartUnmuted { get; set; } = true;
    public bool PlayMuteSound { get; set; }
    public bool MuteOnVrcMute { get; set; } = true;
    public string ModelName { get; set; } = string.Empty;
    public string TtsId { get; set; } = string.Empty;
    public string SpeakerId { get; set; } = string.Empty;
    public int SpeakerVolumeInt { get; set; } = 50;
    public int MaxLenTtsString { get; set; } = 500;
    public bool SkipLongerMessages { get; set; } = true;
    public Dictionary<string, string> VoskModels { get; set; } = [];
    public string VoskModelCurrent { get; set; } = string.Empty;
    public int VoskTimeout { get; set; } = 2500;
    public Dictionary<string, string> WhisperModels { get; set; } = [];
    public string WhisperModelCurrent { get; set; } = string.Empty;
    public bool WhisperSingleSegment { get; set; } = true;
    public bool WhisperToEnglish { get; set; }
    public bool WhisperBracketFix { get; set; } = true;
    public bool WhisperHighPerformance { get; set; }
    public bool WhisperLogFilteredNoises { get; set; }
    public eLanguage WhisperLanguage { get; set; } = eLanguage.English;
    public Dictionary<string, string> WhisperNoiseFilter { get; set; } = [];
    public int WhisperThreads { get; set; } = -4;
    public int WhisperMaxContext { get; set; } = 0;
    public int WhisperMaxSegLen { get; set; } = 0;
    public float WhisperRecMaxDuration { get; set; } = 16;
    public float WhisperRecPauseDuration { get; set; } = 0.5f;
    public string WhisperGraphicsAdapter { get; set; } = string.Empty;
    public string WinModelId { get; set; } = string.Empty;
    public List<string> NoiseFilter { get; set; } = [];
    public bool RemovePeriod { get; set; } = true;
    public bool CapitalizeFirst { get; set; } = true;
    public bool UseReplacements { get; set; } = true;
    public List<LegacyReplacementDataModel> Shortcuts { get; set; } = [];
    public List<LegacyReplacementDataModel> Replacements { get; set; } = [];
    public string ShortcutIgnoredCharacters { get; set; } = ".?!,。、！？";
}

internal class LegacyConfigTextboxModel
{
    public int MaxLength { get; set; } = 130;
    public int TimeoutMultiplier { get; set; } = 1250;
    public int MinimumTimeout { get; set; } = 3000;
    public int DefaultTimeout { get; set; } = 5000;
    public bool DynamicTimeout { get; set; } = true;
    public bool AutomaticClearNotification { get; set; } = true;
    public bool AutomaticClearMessage { get; set; }
    public bool UseIndicatorWhenSpeaking { get; set; }
    public bool UseIndicatorWithoutBox { get; set; }
    public string NotificationIndicatorLeft { get; set; } = "〈";
    public string NotificationIndicatorRight { get; set; } = "〉";
    public bool SoundOnMessage { get; set; } = true;
    public bool SoundOnNotification { get; set; }
    public bool UseNotificationPriority { get; set; } = true;
    public bool UseNotificationSkip { get; set; } = true;
    public bool MediaShowStatus { get; set; }
    public bool MediaAddAlbum { get; set; }
    public bool MediaSwapArtistAndSong { get; set; }
    public string MediaPlayingVerb { get; set; } = "Playing";
    public string MediaArtistVerb { get; set; } = "by";
    public string MediaAlbumVerb { get; set; } = "on";
    public string MediaExtra { get; set; } = string.Empty;
    public List<LegacyFilterModel> MediaFilters { get; set; } = [];
}

internal class LegacyApiPresetModel
{
    public string Name { get; set; } = "Unnamed Preset";
    public string SentData { get; set; } = @"{""data"" : ""[T]""}";
    public Dictionary<string, string> HeaderValues { get; set; } = [];
    public string ContentType { get; set; } = "application/json";
    public string ResultField { get; set; } = "result";
    public string TargetUrl { get; set; } = string.Empty;
    public string Authorization { get; set; } = string.Empty;
    public int ConnectionTimeout { get; set; } = 3000;
}

internal class LegacyAzureTtsVoiceModel
{
    public string Name { get; set; } = "New Voice";
    public string Voice { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

internal class LegacyCounterModel
{
    public string Name { get; set; } = "Unnamed Counter";
    public uint Count { get; set; } = 0;
    public DateTime LastUsed { get; set; } = DateTime.MinValue;
    public bool Enabled { get; set; } = true;
    public float Cooldown { get; set; } = 0;
    public string Parameter { get; set; } = "Parameter";
}

internal class LegacyFilterModel(string name, string filterString)
{
    public string Name { get; set; } = name;
    public string FilterString { get; set; } = filterString;
    public bool Enabled { get; set; } = true;
    public bool IgnoreCase { get; set; } = true;
    public bool UseRegex { get; set; } = false;
}

internal class LegacyOscRoutingFilterModel
{
    public string Name { get; set; } = "Unnamed Filter";
    public int Port { get; set; } = -1;
    public string Ip { get; set; } = "127.0.0.1";
    public List<string> Filters { get; set; } = [];
    public bool BlacklistMode { get; set; } = false;
}

internal class LegacyReplacementDataModel(string text, string replacement, bool ignoreCase = true)
{
    public string Text { get; set; } = text;
    public string Replacement { get; set; } = replacement;
    public bool Enabled { get; set; } = true;
    public bool UseRegex { get; set; } = false;
    public bool IgnoreCase { get; set; } = ignoreCase;
}