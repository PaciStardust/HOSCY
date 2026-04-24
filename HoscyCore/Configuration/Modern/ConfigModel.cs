using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Serilog.Core;
using Serilog.Events;

namespace HoscyCore.Configuration.Modern;

public class ConfigModel : ObservableObject //todo: [FEAT] Ensure all of this is usable from the CLI
{
    public int ConfigVersion { get; set; } = 0;

    #region AFK
    /// <summary>
    /// Show how long afk has been ongoing
    /// </summary>
    public bool Afk_ShowDuration
    {
        get => _afk_ShowDuration;
        set => SetProperty(ref _afk_ShowDuration, value);
    }
    private bool _afk_ShowDuration = false;

    /// <summary>
    /// The base amount of time (in seconds) between displaying afk updates
    /// </summary>
    public float Afk_BaseDurationDisplayIntervalSeconds
    {
        get => _afk_BaseDurationDisplayIntervalSeconds;
        set => SetProperty(ref _afk_BaseDurationDisplayIntervalSeconds, value.MinMax(5, 300));
    }
    private float _afk_BaseDurationDisplayIntervalSeconds = 15f;

    /// <summary>
    /// How often should the afk update be displayed before doubling the time between updates?
    /// </summary>
    public int Afk_TimesDisplayedBeforeDoublingInterval
    {
        get => _afk_TimesDisplayedBeforeDoublingInterval;
        set => SetProperty(ref _afk_TimesDisplayedBeforeDoublingInterval, value.MinMax(1, 60));
    }
    private int _afk_TimesDisplayedBeforeDoublingInterval = 12;

    /// <summary>
    /// Text to display when starting AFK
    /// </summary>
    public string Afk_StartText
    {
        get => _afk_StartText;
        set => SetProperty(ref _afk_StartText, value.Length > 0 ? value : AFK_NO_STARTTEXT);
    }
    private const string AFK_NO_STARTTEXT = "Now AFK";
    private string _afk_StartText = AFK_NO_STARTTEXT;

    /// <summary>
    /// Text to display when stopping AFK
    /// </summary>
    public string Afk_StopText
    {
        get => _afk_EndText;
        set => SetProperty(ref _afk_EndText, value.Length > 0 ? value : AFK_NO_ENDTEXT);
    }
    private const string AFK_NO_ENDTEXT = "No longer AFK";
    private string _afk_EndText = AFK_NO_ENDTEXT;

    /// <summary>
    /// Text to display as AFK update
    /// </summary>
    public string Afk_StatusText
    {
        get => _afk_StatusText;
        set => SetProperty(ref _afk_StatusText, value.Length > 0 ? value : AFK_NO_STATUSTEXT);
    }
    private const string AFK_NO_STATUSTEXT = "AFK since";
    private string _afk_StatusText = AFK_NO_STATUSTEXT;
    #endregion

    #region API
    /// <summary>
    /// List of all API Presets that are used in various locations
    /// </summary>
    public List<ApiPresetModel> Api_Presets
    {
        get => _api_Presets;
        set => SetProperty(ref _api_Presets, value);
    }
    private List<ApiPresetModel> _api_Presets = [];
    public int Api_Presets_GetIndex(string name)
        => Api_Presets.GetListIndex(x => x.Name == name);
    #endregion

    #region Audio
    /// <summary>
    /// ID of microphone
    /// </summary>
    public string Audio_CurrentMicrophoneName
    {
        get => _audio_CurrentMicrophoneName;
        set => SetProperty(ref _audio_CurrentMicrophoneName, value);
    }
    private string _audio_CurrentMicrophoneName = string.Empty;

    /// <summary>
    /// ID of speaker for output audio
    /// </summary>
    public string Audio_CurrentSpeakerOutputName
    {
        get => _audio_CurrentSpeakerOutputName;
        set => SetProperty(ref _audio_CurrentSpeakerOutputName, value);
    }
    private string _audio_CurrentSpeakerOutputName = string.Empty;

    /// <summary>
    /// ID of speaker for system audio
    /// </summary>
    public string Audio_CurrentSpeakerSystemName
    {
        get => _audio_CurrentSpeakerSystemName;
        set => SetProperty(ref _audio_CurrentSpeakerSystemName, value);
    }
    private string _audio_CurrentSpeakerSystemName = string.Empty;
    #endregion

    #region Azure
    /// <summary>
    /// Region of Azure Services
    /// </summary>
    public string AzureServices_Region //todo: [IMPL] To be implemented
    {
        get => _azureServices_Region;
        set => SetProperty(ref _azureServices_Region, value);
    }
    private string _azureServices_Region = string.Empty;

    /// <summary>
    /// API Key to use with Azure
    /// </summary>
    public string AzureServices_ApiKey //todo: [IMPL] To be implemented
    {
        get => _azureServices_ApiKey;
        set => SetProperty(ref _azureServices_ApiKey, value);
    }
    private string _azureServices_ApiKey = string.Empty;
    #endregion

    #region Counters
    /// <summary>
    /// Display counter notifications
    /// </summary>
    public bool Counters_ShowNotification
    {
        get => _counters_ShowNotification;
        set => SetProperty(ref _counters_ShowNotification, value);
    }
    private bool _counters_ShowNotification;

    /// <summary>
    /// How long (in seconds) should a recently triggered counter appear in notifications
    /// </summary>
    public float Counters_DisplayDurationSeconds
    {
        get => _counters_DisplayDurationSeconds;
        set => SetProperty(ref _counters_DisplayDurationSeconds, value.MinMax(0.01f, 30));
    }
    private float _counters_DisplayDurationSeconds = 10f;

    /// <summary>
    /// How long (in seconds) should the timer notification be on cooldown
    /// </summary>
    public float Counters_DisplayCooldownSeconds
    {
        get => _counters_DisplayCooldownSeconds;
        set => SetProperty(ref _counters_DisplayCooldownSeconds, value.MinMax(0, 300));
    }
    private float _counters_DisplayCooldownSeconds = 0f;

    /// <summary>
    /// List of all counters
    /// </summary>
    public List<CounterModel> Counters_List
    {
        get => _counters_List;
        set => SetProperty(ref _counters_List, value);
    }
    private List<CounterModel> _counters_List = [];
    #endregion

    #region Debug
    /// <summary>
    /// Check for updates on startup?
    /// </summary>
    public bool Debug_CheckForUpdatesOnStartup //todo: [IMPL] To be implemented
    {
        get => _debug_CheckForUpdatesOnStartup;
        set => SetProperty(ref _debug_CheckForUpdatesOnStartup, value);
    }
    private bool _debug_CheckForUpdatesOnStartup = true;

    /// <summary>
    /// Open a Windows Debug Window on startup?
    /// </summary>
    public bool Debug_LogViaCmdOnWindows
    {
        get => _debug_LogViaCmdOnWindows;
        set => SetProperty(ref _debug_LogViaCmdOnWindows, value);
    }
    private bool _debug_LogViaCmdOnWindows;

    /// <summary>
    /// Log to terminal when launched in one?
    /// </summary>
    public bool Debug_LogViaTerminal
    {
        get => _debug_LogViaTerminal;
        set => SetProperty(ref _debug_LogViaTerminal, value);
    }
    private bool _debug_LogViaTerminal;

    /// <summary>
    /// Log to a separate terminal by following the log file
    /// </summary>
    public bool Debug_LogViaFileFollow
    {
        get => _debug_LogViaFileFollow;
        set => SetProperty(ref _debug_LogViaFileFollow, value);
    }
    private bool _debug_LogViaFileFollow;

    /// <summary>
    /// Process to use for following the log file
    /// </summary>
    public string Debug_LogFileFollowProcess
    {
        get => _debug_LogFileFollowProcess;
        set => SetProperty(ref _debug_LogFileFollowProcess, value);
    }
    private string _debug_LogFileFollowProcess = "foot";

    /// <summary>
    /// Command to use for following the log file
    /// </summary>
    public string Debug_LogFileFollowCommand
    {
        get => _debug_LogFileFollowCommand;
        set => SetProperty(ref _debug_LogFileFollowCommand, value);
    }
    private string _debug_LogFileFollowCommand = "-e tail -f [LOGFILE]";

    /// <summary>
    /// Minimum log severity to display
    /// </summary>
    public LogEventLevel Debug_LogMinimumSeverity
    {
        get => _debug_LogMinimumSeverity;
        set
        {
            SetProperty(ref _debug_LogMinimumSeverity, value);
            _debug_LogMinimumSeveritySwitch.MinimumLevel = value;
        }
    }
    private readonly LoggingLevelSwitch _debug_LogMinimumSeveritySwitch = new(LogEventLevel.Debug);
    private LogEventLevel _debug_LogMinimumSeverity = LogEventLevel.Debug;
    public LoggingLevelSwitch Debug_LogMinimumSeverityGetSwitch()
        => _debug_LogMinimumSeveritySwitch;

    /// <summary>
    /// Log filters to apply
    /// </summary>
    public List<FilterModel> Debug_LogFilters
    {
        get => _debug_LogFilters;
        set => SetProperty(ref _debug_LogFilters, value);
    }
    private List<FilterModel> _debug_LogFilters = [];

    /// <summary>
    /// Adds extra verbose logging in case it is needed
    /// </summary>
    public bool Debug_LogVerboseExtra
    {
        get => _debug_LogeVerboseExtra;
        set => SetProperty(ref _debug_LogeVerboseExtra, value);
    }
    private bool _debug_LogeVerboseExtra = false;
    #endregion

    #region External Input
    /// <summary>
    /// Can external input trigger commands?
    /// </summary>
    public bool ExternalInput_DoPreprocessFull 
    {
        get => _externalInput_DoPreprocessFull;
        set => SetProperty(ref _externalInput_DoPreprocessFull, value);
    }
    private bool _externalInput_DoPreprocessFull = true;

    /// <summary>
    /// Can external input trigger replacements?
    /// </summary>
    public bool ExternalInput_DoPreprocessPartial
    {
        get => _externalInput_DoPreprocessPartial;
        set => SetProperty(ref _externalInput_DoPreprocessPartial, value);
    }
    private bool _externalInput_DoPreprocessPartial = true;

    /// <summary>
    /// Should input from external sources be translated
    /// </summary>
    public bool ExternalInput_DoTranslate
    {
        get => _externalInput_DoTranslate;
        set => SetProperty(ref _externalInput_DoTranslate, value);
    }
    private bool _externalInput_DoTranslate;
    #endregion

    #region Manual Input
    /// <summary>
    /// Should manual input be sent to audio processors
    /// </summary>
    public bool ManualInput_SendViaAudio
    {
        get => _manualInput_SendViaAudio;
        set => SetProperty(ref _manualInput_SendViaAudio, value);
    }
    private bool _manualInput_SendViaAudio;

    /// <summary>
    /// Should manual input be sent to text processors
    /// </summary>
    public bool ManualInput_SendViaText
    {
        get => _manualInput_SendViaText;
        set => SetProperty(ref _manualInput_SendViaText, value);
    }
    private bool _manualInput_SendViaText = true;

    /// <summary>
    /// Should manual input be sent to other processors
    /// </summary>
    public bool ManualInput_SendViaOther
    {
        get => _manualInput_SendViaOther;
        set => SetProperty(ref _manualInput_SendViaOther, value);
    }
    private bool _manualInput_SendViaOther = true;

    /// <summary>
    /// Can manual input trigger commands?
    /// </summary>
    public bool ManualInput_DoPreprocessFull
    {
        get => _manualInput_DoPreprocessFull;
        set => SetProperty(ref _manualInput_DoPreprocessFull, value);
    }
    private bool _manualInput_DoPreprocessFull = true;

    /// <summary>
    /// Can manual input trigger replacements?
    /// </summary>
    public bool ManualInput_DoPreprocessPartial
    {
        get => _manualInput_DoPreprocessPartial;
        set => SetProperty(ref _manualInput_DoPreprocessPartial, value);
    }
    private bool _manualInput_DoPreprocessPartial = true;

    /// <summary>
    /// Can manual input trigger translation?
    /// </summary>
    public bool ManualInput_DoTranslate
    {
        get => _manualInput_DoTranslate;
        set => SetProperty(ref _manualInput_DoTranslate, value);
    }
    private bool _manualInput_DoTranslate = true;

    /// <summary>
    /// Text presets for manual input
    /// </summary>
    public Dictionary<string, string> ManualInput_TextPresets
    {
        get => _manualInput_TextPresets;
        set => SetProperty(ref _manualInput_TextPresets, value);
    }
    private Dictionary<string, string> _manualInput_TextPresets = [];
    #endregion

    #region Media
    /// <summary>
    /// Show media status as notification
    /// </summary>
    public bool Media_ShowStatus //todo: [IMPL] To be implemented
    {
        get => _media_ShowStatus;
        set => SetProperty(ref _media_ShowStatus, value);
    }
    private bool _media_ShowStatus;

    /// <summary>
    /// Additionally show album text
    /// </summary>
    public bool Media_AddAlbumToText //todo: [IMPL] To be implemented
    {
        get => _media_AddAlbumToText;
        set => SetProperty(ref _media_AddAlbumToText, value);
    }
    private bool _media_AddAlbumToText;

    /// <summary>
    /// Swap artist name and song name
    /// </summary>
    public bool Media_SwapArtistAndSongInText //todo: [IMPL] To be implemented
    {
        get => _media_SwapArtistAndSongInText;
        set => SetProperty(ref _media_SwapArtistAndSongInText, value);
    }
    private bool _media_SwapArtistAndSongInText;

    /// <summary>
    /// Verb used at the start of notification
    /// </summary>
    public string Media_PlayingVerb //todo: [IMPL] To be implemented
    {
        get => _media_PlayingVerb;
        set => SetProperty(ref _media_PlayingVerb, value.Length > 0 ? value : NO_MEDIA_PLAYINGVERB);
    }
    private const string NO_MEDIA_PLAYINGVERB = "Playing";
    private string _media_PlayingVerb = NO_MEDIA_PLAYINGVERB;

    /// <summary>
    /// Word used between artist and song name
    /// </summary>
    public string Media_IntermediateWord //todo: [IMPL] To be implemented
    {
        get => _media_IntermediateWord;
        set => SetProperty(ref _media_IntermediateWord, value.Length > 0 ? value : NO_MEDIA_INTERMEDIATEWORD);
    }
    private const string NO_MEDIA_INTERMEDIATEWORD = "by";
    private string _media_IntermediateWord = NO_MEDIA_INTERMEDIATEWORD;

    /// <summary>
    /// Word used in front of album
    /// </summary>
    public string Media_AlbumWord //todo: [IMPL] To be implemented
    {
        get => _media_AlbumWord;
        set => SetProperty(ref _media_AlbumWord, value.Length > 0 ? value : NO_MEDIA_ALBUMWORD);
    }
    private const string NO_MEDIA_ALBUMWORD = "on";
    private string _media_AlbumWord = NO_MEDIA_ALBUMWORD;

    /// <summary>
    /// Extra text at the end
    /// </summary>
    public string Media_ExtraText //todo: [IMPL] To be implemented
    {
        get => _media_ExtraText;
        set => SetProperty(ref _media_ExtraText, value);
    }
    private string _media_ExtraText = string.Empty;

    /// <summary>
    /// Filtered out words
    /// </summary>
    public List<FilterModel> Media_Filters //todo: [IMPL] To be implemented
    {
        get => _media_Filters;
        set => SetProperty(ref _media_Filters, value);
    }
    private List<FilterModel> _media_Filters = [];
    #endregion

    #region OSC - General
    /// <summary>
    /// Default IP to send OSC to
    /// </summary>
    public string Osc_Routing_TargetIp
    {
        get => _osc_Routing_TargetIp;
        set => SetProperty(ref _osc_Routing_TargetIp, value);
    }
    private string _osc_Routing_TargetIp = "127.0.0.1";

    /// <summary>
    /// Default Port to send OSC to
    /// </summary>
    public ushort Osc_Routing_TargetPort
    {
        get => _osc_Routing_TargetPort;
        set => SetProperty(ref _osc_Routing_TargetPort, value.MinMax(ushort.MinValue, ushort.MaxValue));
    }
    private ushort _osc_Routing_TargetPort = 9000;

    /// <summary>
    /// Port to listen for OSC on
    /// </summary>
    public int Osc_Routing_ListenPort
    {
        get => _osc_Routing_ListenPort;
        set => SetProperty(ref _osc_Routing_ListenPort, value.MinMax(-1, 65535));
    }
    private int _osc_Routing_ListenPort = 9001;

    /// <summary>
    /// List of places to relay OSC to
    /// </summary>
    public List<OscRelayFilterModel> Osc_Relay_Filters
    {
        get => _osc_Relay_Filters;
        set => SetProperty(ref _osc_Relay_Filters, value);
    }
    private List<OscRelayFilterModel> _osc_Relay_Filters = [];

    /// <summary>
    /// Should incoming OSC still be relayed if handled by HOSCY (ex: counters & afk)
    /// </summary>
    public bool Osc_Relay_IgnoreIfHandled
    {
        get => _osc_Relay_IgnoreIfHandled;
        set => SetProperty(ref _osc_Relay_IgnoreIfHandled, value);
    }
    private bool _osc_Relay_IgnoreIfHandled = true;
    #endregion

    #region OSC - Addresses
    /// <summary>
    /// OSC Address to (un)mute recognition when received
    /// </summary>
    public string Osc_Address_Tool_ToggleMute
    {
        get => _osc_Address_Tool_ToggleMute;
        set => SetProperty(ref _osc_Address_Tool_ToggleMute, value);
    }
    private string _osc_Address_Tool_ToggleMute = "/avatar/parameters/ToolMute";

    /// <summary>
    /// OSC Address to skip audio when received
    /// </summary>
    public string Osc_Address_Tool_SkipAudio //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Tool_SkipAudio;
        set => SetProperty(ref _osc_Address_Tool_SkipAudio, value);
    }
    private string _osc_Address_Tool_SkipAudio = "/avatar/parameters/ToolSkipSpeech";

    /// <summary>
    /// OSC Address to skip text when received
    /// </summary>
    public string Osc_Address_Tool_SkipText //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Tool_SkipText;
        set => SetProperty(ref _osc_Address_Tool_SkipText, value);
    }
    private string _osc_Address_Tool_SkipText = "/avatar/parameters/ToolSkipBox";

    /// <summary>
    /// OSC Address to toggle replacements
    /// </summary>
    public string Osc_Address_Tool_ToggleReplacements //todo: [IMPL] To be implemented or changed
    {
        get => _osc_Address_Tool_ToggleReplacements;
        set => SetProperty(ref _osc_Address_Tool_ToggleReplacements, value);
    }
    private string _osc_Address_Tool_ToggleReplacements = "/avatar/parameters/ToolEnableReplacements";

    /// <summary>
    /// OSC Address to toggle to output to text
    /// </summary>
    public string Osc_Address_Tool_ToogleOutputToText //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Tool_ToggleOutputToText;
        set => SetProperty(ref _osc_Address_Tool_ToggleOutputToText, value);
    }
    private string _osc_Address_Tool_ToggleOutputToText = "/avatar/parameters/ToolEnableBox";

    /// <summary>
    /// OSC Address to toggle to output to other
    /// </summary>
    public string Osc_Address_Tool_ToogleSpeechToOther //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Tool_ToggleOutputToOther;
        set => SetProperty(ref _osc_Address_Tool_ToggleOutputToOther, value);
    }
    private string _osc_Address_Tool_ToggleOutputToOther = "/avatar/parameters/ToolEnableOther";

    /// <summary>
    /// OSC Address to toggle to output to audio
    /// </summary>
    public string Osc_Address_Tool_ToggleOutputToAudio //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Tool_ToggleOutputToAudio;
        set => SetProperty(ref _osc_Address_Tool_ToggleOutputToAudio, value);
    }
    private string _osc_Address_Tool_ToggleOutputToAudio = "/avatar/parameters/ToolEnableTts";

    /// <summary>
    /// OSC Address to toggle recognition auto mute
    /// </summary>
    public string Osc_Address_Tool_ToggleRecognitionAutoMute
    {
        get => _osc_Address_Tool_ToggleRecognitionAutoMute;
        set => SetProperty(ref _osc_Address_Tool_ToggleRecognitionAutoMute, value);
    }
    private string _osc_Address_Tool_ToggleRecognitionAutoMute = "/avatar/parameters/ToolEnableAutoMute";

    /// <summary>
    /// OSC Address sent out when recognition status changes
    /// </summary>
    public string Osc_Address_Tool_NotificationForRecognitionListening //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Tool_NotificationForRecognitionListening;
        set => SetProperty(ref _osc_Address_Tool_NotificationForRecognitionListening, value);
    }
    private string _osc_Address_Tool_NotificationForRecognitionListening = "/avatar/parameters/MicListening";

    /// <summary>
    /// OSC Address the game sends out when muted
    /// </summary>
    public string Osc_Address_Game_Mute
    {
        get => _osc_Address_Game_Mute;
        set => SetProperty(ref _osc_Address_Game_Mute, value);
    }
    private string _osc_Address_Game_Mute = "/avatar/parameters/MuteSelf";

    /// <summary>
    /// OSC Address the game sends out when afk
    /// </summary>
    public string Osc_Address_Game_Afk
    {
        get => _osc_Address_Game_Afk;
        set => SetProperty(ref _osc_Address_Game_Afk, value);
    }
    private string _osc_Address_Game_Afk = "/avatar/parameters/AFK";

    /// <summary>
    /// OSC Address the game listens to for textbox usage
    /// </summary>
    public string Osc_Address_Game_Textbox
    {
        get => _osc_Address_Game_Textbox;
        set => SetProperty(ref _osc_Address_Game_Textbox, value);
    }
    private string _osc_Address_Game_Textbox = "/chatbox/input";

    /// <summary>
    /// OSC Address the game listens to for typing indicator
    /// </summary>
    public string Osc_Address_Game_Typing
    {
        get => _osc_Address_Game_Typing;
        set => SetProperty(ref _osc_Address_Game_Typing, value);
    }
    private string _osc_Address_Game_Typing = "/chatbox/typing";

    /// <summary>
    /// OSC Address to send to tool for external messages to be sent as text
    /// </summary>
    public string Osc_Address_Input_TextMessage
    {
        get => _osc_Address_Input_TextMessage;
        set => SetProperty(ref _osc_Address_Input_TextMessage, value);
    }
    private string _osc_Address_Input_TextMessage = "/hoscy/message";

    /// <summary>
    /// OSC Address to send to tool for external notifications to be sent as text
    /// </summary>
    public string Osc_Address_Input_TextNotification
    {
        get => _osc_Address_Input_TextNotification;
        set => SetProperty(ref _osc_Address_Input_TextNotification, value);
    }
    private string _osc_Address_Input_TextNotification = "/hoscy/notification";

    /// <summary>
    /// OSC Address to send to tool for external text to be sent as audio
    /// </summary>
    public string Osc_Address_Input_AudioMessage
    {
        get => _osc_Address_Input_AudioMessage;
        set => SetProperty(ref _osc_Address_Input_AudioMessage, value);
    }
    private string _osc_Address_Input_AudioMessage = "/hoscy/tts";

    /// <summary>
    /// OSC Address to send to tool for external text to be sent as other
    /// </summary>
    public string Osc_Address_Input_OtherMessage
    {
        get => _osc_Address_Input_OtherMessage;
        set => SetProperty(ref _osc_Address_Input_OtherMessage, value);
    }
    private string _osc_Address_Input_OtherMessage = "/hoscy/other";

    /// <summary>
    /// OSC Address to send to tool to pause media
    /// </summary>
    public string Osc_Address_Media_Pause //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Media_Pause;
        set => SetProperty(ref _osc_Address_Media_Pause, value);
    }
    private string _osc_Address_Media_Pause = "/avatar/parameters/MediaPause";

    /// <summary>
    /// OSC Address to send to tool to unpause media
    /// </summary>
    public string Osc_Address_Media_Unpause //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Media_Unpause;
        set => SetProperty(ref _osc_Address_Media_Unpause, value);
    }
    private string _osc_Address_Media_Unpause = "/avatar/parameters/MediaUnpause";

    /// <summary>
    /// OSC Address to send to tool to rewind media
    /// </summary>
    public string Osc_Address_Media_Rewind //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Media_Rewind;
        set => SetProperty(ref _osc_Address_Media_Rewind, value);
    }
    private string _osc_Address_Media_Rewind = "/avatar/parameters/MediaRewind";

    /// <summary>
    /// OSC Address to send to tool to skip media
    /// </summary>
    public string Osc_Address_Media_Skip //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Media_Skip;
        set => SetProperty(ref _osc_Address_Media_Skip, value);
    }
    private string _osc_Address_Media_Skip = "/avatar/parameters/MediaSkip";

    /// <summary>
    /// OSC Address to send to tool to display media info
    /// </summary>
    public string Osc_Address_Media_Info //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Media_Info;
        set => SetProperty(ref _osc_Address_Media_Info, value);
    }
    private string _osc_Address_Media_Info = "/avatar/parameters/MediaInfo";

    /// <summary>
    /// OSC Address to send to tool to toggle media playback
    /// </summary>
    public string Osc_Address_Media_Toggle //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Media_Toggle;
        set => SetProperty(ref _osc_Address_Media_Toggle, value);
    }
    private string _osc_Address_Media_Toggle = "/avatar/parameters/MediaToggle";
    #endregion

    #region Preprocessing
    /// <summary>
    /// Enables/Disables partial replacements entirely
    /// </summary>
    public bool Preprocessing_DoReplacementsPartial
    {
        get => _preprocessing_DoReplacementsPartial;
        set => SetProperty(ref _preprocessing_DoReplacementsPartial, value);
    }
    private bool _preprocessing_DoReplacementsPartial = true;

    /// <summary>
    /// Enables/Disables full replacements entirely
    /// </summary>
    public bool Preprocessing_DoReplacementsFull
    {
        get => _preprocessing_DoReplacementsFull;
        set => SetProperty(ref _preprocessing_DoReplacementsFull, value);
    }
    private bool _preprocessing_DoReplacementsFull = true;

    /// <summary>
    /// List of full replacements to use
    /// </summary>
    public List<ReplacementDataModel> Preprocessing_ReplacementsFull
    {
        get => _preprocessing_ReplacementsFull;
        set => SetProperty(ref _preprocessing_ReplacementsFull, value);
    }
    private List<ReplacementDataModel> _preprocessing_ReplacementsFull = [];

    /// <summary>
    /// List of partial replacements to use
    /// </summary>
    public List<ReplacementDataModel> Preprocessing_ReplacementsPartial
    {
        get => _preprocessing_ReplacementsPartial;
        set => SetProperty(ref _preprocessing_ReplacementsPartial, value);
    }
    private List<ReplacementDataModel> _preprocessing_ReplacementsPartial = [];

    /// <summary>
    /// Characters that get ignored for full replacements
    /// </summary>
    public string Preprocessing_ReplacementFullIgnoredCharacters
    {
        get => _preprocessing_ReplacementFullIgnoredCharacters;
        set => SetProperty(ref _preprocessing_ReplacementFullIgnoredCharacters, value);
    }
    private string _preprocessing_ReplacementFullIgnoredCharacters = ".?!,。、！？";
    #endregion

    #region Output - API
    public bool ApiOut_Enabled
    {
        get => _apiOut_Enabled;
        set => SetProperty(ref _apiOut_Enabled, value);
    }
    private bool _apiOut_Enabled = false;

    public string ApiOut_Preset_Message
    {
        get => _apiOut_Preset_Message;
        set => SetProperty(ref _apiOut_Preset_Message, value);
    }
    private string _apiOut_Preset_Message = string.Empty;

    public string ApiOut_Preset_Notification
    {
        get => _apiOut_Preset_Notification;
        set => SetProperty(ref _apiOut_Preset_Notification, value);
    }
    private string _apiOut_Preset_Notification = string.Empty;

    public string ApiOut_Preset_Clear
    {
        get => _apiOut_Preset_Clear;
        set => SetProperty(ref _apiOut_Preset_Clear, value);
    }
    private string _apiOut_Preset_Clear = string.Empty;

    public string ApiOut_Preset_Processing
    {
        get => _apiOut_Preset_Processing;
        set => SetProperty(ref _apiOut_Preset_Processing, value);
    }
    private string _apiOut_Preset_Processing = string.Empty;

    public string ApiOut_Value_True
    {
        get => _apiOut_Value_True;
        set => SetProperty(ref _apiOut_Value_True, value);
    }
    private string _apiOut_Value_True = string.Empty;

    public string ApiOut_Value_False
    {
        get => _apiOut_Value_False;
        set => SetProperty(ref _apiOut_Value_False, value);
    }
    private string _apiOut_Value_False = string.Empty;

    public OutputTranslationFormat ApiOut_TranslationFormat
    {
        get => _apiOut_TranslationFormat;
        set => SetProperty(ref _apiOut_TranslationFormat, value);
    }
    private OutputTranslationFormat _apiOut_TranslationFormat = OutputTranslationFormat.Both;
    #endregion

    #region Output - VRC Textbox
    /// <summary>
    /// Should the Textbox be enabled
    /// </summary>
    public bool VrcTextbox_Enabled
    {
        get => _vrcTextbox_Enabled;
        set => SetProperty(ref _vrcTextbox_Enabled, value);
    }
    private bool _vrcTextbox_Enabled = false;

    /// <summary>
    /// Should translated content be sent to the VRC Textbox?
    /// </summary>
    public bool VrcTextbox_Output_ShowTranslation
    {
        get => _vrcTextbox_Output_ShowTranslation;
        set => SetProperty(ref _vrcTextbox_Output_ShowTranslation, value);
    }
    private bool _vrcTextbox_Output_ShowTranslation;

    /// <summary>
    /// Should original be added after translation?
    /// </summary>
    public bool VrcTextbox_Output_AddOriginalToTranslation
    {
        get => _vrcTextbox_Output_AddOriginalToTranslation;
        set => SetProperty(ref _vrcTextbox_Output_AddOriginalToTranslation, value);
    }
    private bool _vrcTextbox_Output_AddOriginalToTranslation = true;

    /// <summary>
    /// Maximum of characters displayed
    /// </summary>
    public int VrcTextbox_Output_MaxDisplayedCharacters
    {
        get => _vrcTextbox_Output_MaxDisplayedCharacters;
        set => SetProperty(ref _vrcTextbox_Output_MaxDisplayedCharacters, value.MinMax(10, 130));
    }
    private int _vrcTextbox_Output_MaxDisplayedCharacters = 130;

    /// <summary>
    /// Actuall output text (disable to only have processing indicator)
    /// </summary>
    public bool VrcTextbox_Do_Output
    {
        get => _vrcTextbox_Do_Output; 
        set => SetProperty(ref _vrcTextbox_Do_Output, value);
    }
    private bool _vrcTextbox_Do_Output = true;

    /// <summary>
    /// Show indicator while processing
    /// </summary>
    public bool VrcTextbox_Do_Indicator
    {
        get => _vrcTextbox_Do_Indicator;
        set => SetProperty(ref _vrcTextbox_Do_Indicator, value);
    }
    private bool _vrcTextbox_Do_Indicator = true;

    /// <summary>
    /// Ms of timeout per 20 characters displayed at same time
    /// </summary>
    public int VrcTextbox_Timeout_DynamicPer20CharactersDisplayedMs
    {
        get => _vrcTextbox_Timeout_DynamicPer20CharactersDisplayedMs;
        set => SetProperty(ref _vrcTextbox_Timeout_DynamicPer20CharactersDisplayedMs, value.MinMax(250, 10000));
    }
    private int _vrcTextbox_Timeout_DynamicPer20CharactersDisplayedMs = 1250;

    /// <summary>
    /// Minimum timeout in ms when using dynamic timeout
    /// </summary>
    public int VrcTextbox_Timeout_DynamicMinimumMs
    {
        get => _vrcTextbox_Timeout_DynamicMinimumMs;
        set => SetProperty(ref _vrcTextbox_Timeout_DynamicMinimumMs, value.MinMax(1250, 30000));
    }
    private int _vrcTextbox_Timeout_DynamicMinimumMs = 3000;

    /// <summary>
    /// Timeout in ms when using static timeout
    /// </summary>
    public int VrcTextbox_Timeout_StaticMs
    {
        get => _vrcTextbox_Timeout_StaticMs;
        set => SetProperty(ref _vrcTextbox_Timeout_StaticMs, value.MinMax(1250, 30000));
    }
    private int _vrcTextbox_Timeout_StaticMs = 5000;

    /// <summary>
    /// Use dynamic display timeout
    /// </summary>
    public bool VrcTextbox_Timeout_UseDynamic
    {
        get => _vrcTextbox_Timeout_UseDynamic;
        set => SetProperty(ref _vrcTextbox_Timeout_UseDynamic, value);
    }
    private bool _vrcTextbox_Timeout_UseDynamic = true;

    /// <summary>
    /// Automatically clear after notifications
    /// </summary>
    public bool VrcTextbox_Timeout_AutomaticallyClearNotification
    {
        get => _vrcTextbox_Timeout_AutomaticallyClearNotification;
        set => SetProperty(ref _vrcTextbox_Timeout_AutomaticallyClearNotification, value);
    }
    private bool _vrcTextbox_Timeout_AutomaticallyClearNotification = true;

    /// <summary>
    /// Automatically clear after message
    /// </summary>
    public bool VrcTextbox_Timeout_AutomaticallyClearMessage
    {
        get => _vrcTextbox_Timeout_AutomaticallyClearMessage;
        set => SetProperty(ref _vrcTextbox_Timeout_AutomaticallyClearMessage, value);
    }
    private bool _vrcTextbox_Timeout_AutomaticallyClearMessage;

    /// <summary>
    /// Text to the left of a notification
    /// </summary>
    public string VrcTextbox_Notification_IndicatorTextStart
    {
        get => _vrcTextbox_Notification_IndicatorTextStart;
        set => SetProperty(ref _vrcTextbox_Notification_IndicatorTextStart, value.Length < 4 ? value : value[..3]);
    }
    /// <summary>
    /// Text to the right of a notification
    /// </summary>
    public string VrcTextbox_Notification_IndicatorTextEnd
    {
        get => _vrcTextbox_Notification_IndicatorTextEnd;
        set => SetProperty(ref _vrcTextbox_Notification_IndicatorTextEnd, value.Length < 4 ? value : value[..3]);
    }
    private string _vrcTextbox_Notification_IndicatorTextStart = "〈";
    private string _vrcTextbox_Notification_IndicatorTextEnd = "〉";

    /// <summary>
    /// Use notification priority system
    /// </summary>
    public bool VrcTextbox_Notification_UsePrioritySystem
    {
        get => _vrcTextbox_Notification_UsePrioritySystem;
        set => SetProperty(ref _vrcTextbox_Notification_UsePrioritySystem, value);
    }
    private bool _vrcTextbox_Notification_UsePrioritySystem = true;

    /// <summary>
    /// Skip notifications when there is an available message
    /// </summary>
    public bool VrcTextbox_Notification_SkipWhenMessageAvailable
    {
        get => _vrcTextbox_Notification_SkipWhenMessageAvailable;
        set => SetProperty(ref _vrcTextbox_Notification_SkipWhenMessageAvailable, value);
    }
    private bool _vrcTextbox_Notification_SkipWhenMessageAvailable = true;

    /// <summary>
    /// Play sound on message
    /// </summary>
    public bool VrcTextbox_Sound_OnMessage
    {
        get => _vrcTextbox_Sound_OnMessage;
        set => SetProperty(ref _vrcTextbox_Sound_OnMessage, value);
    }
    private bool _vrcTextbox_Sound_OnMessage = true;

    /// <summary>
    /// Play sound on notification
    /// </summary>
    public bool VrcTextbox_Sound_OnNotification
    {
        get => _vrcTextbox_Sound_OnNotification;
        set => SetProperty(ref _vrcTextbox_Sound_OnNotification, value);
    }
    private bool _vrcTextbox_Sound_OnNotification;
    #endregion

    #region Recognition - General
    /// <summary>
    /// Allow sending recognition result over text
    /// </summary>
    public bool Recognition_Send_ViaText
    {
        get => _recognition_Send_ViaText;
        set => SetProperty(ref _recognition_Send_ViaText, value);
    }
    private bool _recognition_Send_ViaText = true;

    /// <summary>
    /// Allow sending recognition result over audio
    /// </summary>
    public bool Recognition_Send_ViaAudio
    {
        get => _recognition_Send_ViaAudio;
        set => SetProperty(ref _recognition_Send_ViaAudio, value);
    }
    private bool _recognition_Send_ViaAudio = false;

    /// <summary>
    /// Allow sending recognition result over other
    /// </summary>
    public bool Recognition_Send_ViaOther
    {
        get => _recognition_Send_ViaOther;
        set => SetProperty(ref _recognition_Send_ViaOther, value);
    }
    private bool _recognition_Send_ViaOther = false;

    /// <summary>
    /// Allow translation of recognition result
    /// </summary>
    public bool Recognition_Send_DoTranslate
    {
        get => _recognition_Send_DoTranslate;
        set => SetProperty(ref _recognition_Send_DoTranslate, value);
    }
    private bool _recognition_Send_DoTranslate = false;

    /// <summary>
    /// Allow partial preprocessing of recognition result
    /// </summary>
    public bool Recognition_Send_DoPreprocessPartial
    {
        get => _recognition_Send_DoPreprocessPartial;
        set => SetProperty(ref _recognition_Send_DoPreprocessPartial, value);
    }
    private bool _recognition_Send_DoPreprocessPartial = true;

    /// <summary>
    /// Allow full preprocessing of recognition result
    /// </summary>
    public bool Recognition_Send_DoPreprocessFull
    {
        get => _recognition_Send_DoPreprocessFull;
        set => SetProperty(ref _recognition_Send_DoPreprocessFull, value);
    }
    private bool _recognition_Send_DoPreprocessFull = true;

    /// <summary>
    /// Should recognition be unmuted when started
    /// </summary>
    public bool Recognition_Mute_StartUnmuted
    {
        get => _recognition_Mute_StartUnmuted;
        set => SetProperty(ref _recognition_Mute_StartUnmuted, value);
    }
    private bool _recognition_Mute_StartUnmuted = true;

    /// <summary>
    /// Should a sound be played on (un)mute
    /// </summary>
    public bool Recognition_Mute_PlaySound //todo: [IMPL] To be implemented
    {
        get => _recognition_Mute_PlaySound;
        set => SetProperty(ref _recognition_Mute_PlaySound, value);
    }
    private bool _recognition_Mute_PlaySound = false;

    /// <summary>
    /// Should recognition be muted when the game is muted
    /// </summary>
    public bool Recognition_Mute_OnGameMute //todo: [IMPL] To be implemented
    {
        get => _recognition_Mute_OnGameMute;
        set => SetProperty(ref _recognition_Mute_OnGameMute, value);
    }
    private bool _recognition_Mute_OnGameMute = true;

    /// <summary>
    /// Module to use for recognition
    /// </summary>
    public string Recognition_SelectedModuleName
    {
        get => _recognition_SelectedModuleName;
        set => SetProperty(ref _recognition_SelectedModuleName, value);
    }
    private string _recognition_SelectedModuleName = string.Empty;

    /// <summary>
    /// Should recognition start automatically
    /// </summary>
    public bool Recognition_AutoStart
    {
        get => _recognition_AutoStart;
        set => SetProperty(ref _recognition_AutoStart, value);
    }
    private bool _recognition_AutoStart = false;

    /// <summary>
    /// Noise filtered out by recognizers
    /// </summary>
    public HashSet<string> Recognition_Fixup_NoiseFilter
    {
        get => _recognition_Fixup_NoiseFilter;
        set => SetProperty(ref _recognition_Fixup_NoiseFilter, value);
    }
    private HashSet<string> _recognition_Fixup_NoiseFilter = [];

    /// <summary>
    /// Remove the period at the end of a message
    /// </summary>
    public bool Recognition_Fixup_RemoveEndPeriod
    {
        get => _recognition_Fixup_RemoveEndPeriod;
        set => SetProperty(ref _recognition_Fixup_RemoveEndPeriod, value);
    }
    private bool _recognition_Fixup_RemoveEndPeriod = true;

    /// <summary>
    /// Capitalizes the first character of the message
    /// </summary>
    public bool Recognition_Fixup_CapitalizeFirstLetter
    {
        get => _recognition_Fixup_CapitalizeFirstLetter;
        set => SetProperty(ref _recognition_Fixup_CapitalizeFirstLetter, value);
    }
    private bool _recognition_Fixup_CapitalizeFirstLetter = true;
    #endregion

    #region Recognition - API
    /// <summary>
    /// API Preset for API Speech Recognition
    /// </summary>
    public string Recognition_Api_Preset //todo: [IMPL] To be implemented
    {
        get => _recognition_Api_Preset;
        set => SetProperty(ref _recognition_Api_Preset, value);
    }
    private string _recognition_Api_Preset = string.Empty;

    /// <summary>
    /// Maximum recording time for API Speech Recognition in seconds 
    /// </summary>
    public int Recognition_Api_MaxRecordingTime //todo: [IMPL] To be implemented
    {
        get => _recognition_Api_MaxRecordingTime;
        set => SetProperty(ref _recognition_Api_MaxRecordingTime, value.MinMax( 1, 300));
    }
    private int _recognition_Api_MaxRecordingTime = 30;
    #endregion

    #region Recognition - Azure
    /// <summary>
    /// Custom endpoint for Azure Speech Recognition
    /// </summary>
    public string Recognition_Azure_CustomEndpoint //todo: [IMPL] To be implemented
    {
        get => _recognition_Azure_CustomEndpoint;
        set => SetProperty(ref _recognition_Azure_CustomEndpoint, value);
    }
    private string _recognition_Azure_CustomEndpoint = string.Empty;

    /// <summary>
    /// Preset phrases for Azure Speech Recognition
    /// </summary>
    public HashSet<string> Recognition_Azure_PresetPhrases //todo: [IMPL] To be implemented
    {
        get => _recognition_Azure_Phrases;
        set => SetProperty(ref _recognition_Azure_Phrases, value);
    }
    private HashSet<string> _recognition_Azure_Phrases = [];

    /// <summary>
    /// List of Languages for Azure Speech Recognition
    /// </summary>
    public HashSet<string> Recognition_Azure_Languages //todo: [IMPL] To be implemented
    {
        get => _recognition_Azure_Languages;
        set => SetProperty(ref _recognition_Azure_Languages, value);
    }
    private HashSet<string> _recognition_Azure_Languages = [];
    #endregion

    #region Recognition - Vosk
    /// <summary>
    /// List of available vosk models with file path
    /// </summary>
    public Dictionary<string, string> Recognition_Vosk_Models //todo: [IMPL] To be implemented
    {
        get => _recognition_Vosk_Models;
        set => SetProperty(ref _recognition_Vosk_Models, value);
    }
    private Dictionary<string, string> _recognition_Vosk_Models = [];

    /// <summary>
    /// Currently used vosk model
    /// </summary>
    public string Recognition_Vosk_CurrentModel //todo: [IMPL] To be implemented
    {
        get => _recognition_Vosk_CurrentModel;
        set => SetProperty(ref _recognition_Vosk_CurrentModel, value);
    }
    private string _recognition_Vosk_CurrentModel = string.Empty;

    /// <summary>
    /// Time to wait in MS for new word before stopping sentence
    /// </summary>
    public int Recognition_Vosk_NewWordWaitTimeMs //todo: [IMPL] To be implemented
    {
        get => _recognition_Vosk_NewWordWaitTimeMs;
        set => SetProperty(ref _recognition_Vosk_NewWordWaitTimeMs,value.MinMax(500, 30000));
    }
    private int _recognition_Vosk_NewWordWaitTimeMs = 2500;
    #endregion

    #region Recognition - Whisper
    /// <summary>
    /// List of whisper models with file path
    /// </summary>
    public Dictionary<string, string> Recognition_Whisper_Models
    {
        get => _recognition_Whisper_Models;
        set => SetProperty(ref _recognition_Whisper_Models, value);
    }
    private Dictionary<string, string> _recognition_Whisper_Models = [];

    /// <summary>
    /// Currently in use whisper model
    /// </summary>
    public string Recognition_Whisper_SelectedModel
    {
        get => _recognition_Whisper_SelectedModel;
        set => SetProperty(ref _recognition_Whisper_SelectedModel, value);
    }
    private string _recognition_Whisper_SelectedModel = string.Empty;

    /// <summary>
    /// Fixes random brackets in the output "('( ( (')"
    /// </summary>
    public bool Recognition_Whisper_Fix_RemoveRandomBrackets
    {
        get => _recognition_Whisper_Fix_RemoveRandomBrackets;
        set => SetProperty(ref _recognition_Whisper_Fix_RemoveRandomBrackets, value);
    }
    private bool _recognition_Whisper_Fix_RemoveRandomBrackets = true;

    /// <summary>
    /// Write noises that have been filtered out to the logs
    /// </summary>
    public bool Recognition_Whisper_Dbg_LogFilteredNoises
    {
        get => _recognition_Whisper_Dbg_LogFilteredNoises;
        set => SetProperty(ref _recognition_Whisper_Dbg_LogFilteredNoises, value);
    }
    private bool _recognition_Whisper_Dbg_LogFilteredNoises = false;

    /// <summary>
    /// List of allowed whisper noises
    /// </summary>
    public Dictionary<string, string> Recognition_Whisper_Cfg_NoiseFilter
    {
        get => _recognition_Whisper_Cfg_NoiseFilter;
        set => SetProperty(ref _recognition_Whisper_Cfg_NoiseFilter, value);
    }
    private Dictionary<string, string> _recognition_Whisper_Cfg_NoiseFilter = [];

    /// <summary>
    /// Enable single segment mode for higher accuracy but reduced functionality
    /// </summary>
    public bool Recognition_Whisper_Cfg_UseSingleSegmentMode
    {
        get => _recognition_Whisper_Cfg_UseSingleSegmentMode;
        set => SetProperty(ref _recognition_Whisper_Cfg_UseSingleSegmentMode, value);
    }
    private bool _recognition_Whisper_Cfg_UseSingleSegmentMode = true;

    /// <summary>
    /// Translate to English if the detected language is not English
    /// </summary>
    public bool Recognition_Whisper_Cfg_TranslateToEnglish
    {
        get => _recognition_Whisper_Cfg_TranslateToEnglish;
        set => SetProperty(ref _recognition_Whisper_Cfg_TranslateToEnglish, value);
    }
    private bool _recognition_Whisper_Cfg_TranslateToEnglish = false;

    /// <summary>
    /// Translate to English if the detected language is not English
    /// </summary>
    public bool Recognition_Whisper_Cfg_UseGpu
    {
        get => _recognition_Whisper_Cfg_UseGpu;
        set => SetProperty(ref _recognition_Whisper_Cfg_UseGpu, value);
    }
    private bool _recognition_Whisper_Cfg_UseGpu = true;

    /// <summary>
    /// Should language automatically be detected?
    /// </summary>
    public bool Recognition_Whisper_Cfg_DetectLanguage
    {
        get => _recognition_Whisper_Cfg_DetectLanguage;
        set => SetProperty(ref _recognition_Whisper_Cfg_DetectLanguage, value);
    }
    private bool _recognition_Whisper_Cfg_DetectLanguage = false;

    /// <summary>
    /// Shortcode for language
    /// </summary>
    public string Recognition_Whisper_Cfg_Language
    {
        get => _recognition_Whisper_Cfg_Language;
        set => SetProperty(ref _recognition_Whisper_Cfg_Language, value);
    }
    private string _recognition_Whisper_Cfg_Language = string.Empty;

    /// <summary>
    /// Cutoff time for a sentence in MS
    /// </summary>
    public int Recognition_Whisper_Cfg_MaxSentenceDurationMs
    {
        get => _recognition_Whisper_Cfg_MaxSentenceDurationMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_MaxSentenceDurationMs, value.MinMax(4_000, int.MaxValue));
    }
    private int _recognition_Whisper_Cfg_MaxSentenceDurationMs = 16_000;

    /// <summary>
    /// Minimum time for a sentence in MS
    /// </summary>
    public int Recognition_Whisper_Cfg_MinSentenceDurationMs
    {
        get => _recognition_Whisper_Cfg_MinSentenceDurationMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_MinSentenceDurationMs, value.MinMax(100, 2_000));
    }
    private int _recognition_Whisper_Cfg_MinSentenceDurationMs = 250;

    /// <summary>
    /// Duration to recognize a pause in MS
    /// </summary>
    public int Recognition_Whisper_Cfg_DetectPauseDurationMs
    {
        get => _recognition_Whisper_Cfg_DetectPauseDurationMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_DetectPauseDurationMs, value.MinMax(250, 2_000));
    }
    private int _recognition_Whisper_Cfg_DetectPauseDurationMs = 500;

    /// <summary>
    /// Duration to recognize a silence in outer segments in MS
    /// </summary>
    public int Recognition_Whisper_Cfg_DetectOuterSilenceDurationMs
    {
        get => _recognition_Whisper_Cfg_DetectOuterSilenceDurationMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_DetectOuterSilenceDurationMs, value.MinMax(100, 1000));
    }
    private int _recognition_Whisper_Cfg_DetectOuterSilenceDurationMs = 250;

    /// <summary>
    /// How often in MS should recognition be updated in between? Lower means more processing
    /// </summary>
    public int Recognition_Whisper_Cfg_RecognitionUpdateIntervalMs
    {
        get => _recognition_Whisper_Cfg_RecognitionUpdateIntervalMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_RecognitionUpdateIntervalMs, value.MinMax(250, 4_000));
    }
    private int _recognition_Whisper_Cfg_RecognitionUpdateIntervalMs = 500;

    /// <summary>
    /// Operating mode for voice activity detection
    /// </summary>
    public WhisperIpcVadOperatingMode Recognition_Whisper_Cfg_VadOperatingMode
    {
        get => _recognition_Whisper_Cfg_VadOperatingMode;
        set => SetProperty(ref _recognition_Whisper_Cfg_VadOperatingMode, value);
    }
    private WhisperIpcVadOperatingMode _recognition_Whisper_Cfg_VadOperatingMode = WhisperIpcVadOperatingMode.Aggressive;

    /// <summary>
    /// Beam size for beam search sampling strategy
    /// </summary>
    public int Recognition_Whisper_CfgAdv_BeamSize
    {
        get => _recognition_Whisper_CfgAdv_BeamSize;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_BeamSize, value.MinMax(0, 10));
    }
    private int _recognition_Whisper_CfgAdv_BeamSize = 0;

    /// <summary>
    /// Best of for greedy sampling strategy
    /// </summary>
    public int Recognition_Whisper_CfgAdv_GreedyBestOf
    {
        get => _recognition_Whisper_CfgAdv_GreedyBestOf;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_GreedyBestOf, value.MinMax(0, 10));
    }
    private int _recognition_Whisper_CfgAdv_GreedyBestOf = 0;

    /// <summary>
    /// GPU to use
    /// </summary>
    public int Recognition_Whisper_CfgAdv_GraphicsAdapterId
    {
        get => _recognition_Whisper_CfgAdv_GraphicsAdapterId;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_GraphicsAdapterId, value.MinMax(0, int.MaxValue));
    }
    private int _recognition_Whisper_CfgAdv_GraphicsAdapterId = 0;

    /// <summary>
    /// MaxInitialT for Whisper
    /// </summary>
    public float Recognition_Whisper_CfgAdv_MaxInitialT
    {
        get => _recognition_Whisper_CfgAdv_MaxInitialT;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_MaxInitialT, value.MinMax(-1, 1));
    }
    private float _recognition_Whisper_CfgAdv_MaxInitialT = -1;

    /// <summary>
    /// No speech threshold for Whisper
    /// </summary>
    public float Recognition_Whisper_CfgAdv_NoSpeechThreshold
    {
        get => _recognition_Whisper_CfgAdv_NoSpeechThreshold;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_NoSpeechThreshold, value.MinMax(-1, 1));
    }
    private float _recognition_Whisper_CfgAdv_NoSpeechThreshold = -1;

    /// <summary>
    /// Temperature for Whisper
    /// </summary>
    public float Recognition_Whisper_CfgAdv_Temperature
    {
        get => _recognition_Whisper_CfgAdv_Temperature;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_Temperature, value.MinMax(-1, 1));
    }
    private float _recognition_Whisper_CfgAdv_Temperature = -1;

    /// <summary>
    /// TemperatureInc for Whisper
    /// </summary>
    public float Recognition_Whisper_CfgAdv_TemperatureInc
    {
        get => _recognition_Whisper_CfgAdv_TemperatureInc;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_TemperatureInc, value.MinMax(-1, 1));
    }
    private float _recognition_Whisper_CfgAdv_TemperatureInc = -1;

    /// <summary>
    /// Maximum segment length
    /// </summary>
    public int Recognition_Whisper_CfgAdv_MaxSegmentLength
    {
        get => _recognition_Whisper_CfgAdv_MaxSegmentLength;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_MaxSegmentLength, value.MinMax(0, int.MaxValue));
    }
    private int _recognition_Whisper_CfgAdv_MaxSegmentLength = 0;

    /// <summary>
    /// Maxiumum amount of tokens per segment
    /// </summary>
    public int Recognition_Whisper_CfgAdv_MaxTokensPerSegment
    {
        get => _recognition_Whisper_CfgAdv_MaxTokensPerSegment;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_MaxTokensPerSegment, value.MinMax(0, int.MaxValue));
    }
    private int _recognition_Whisper_CfgAdv_MaxTokensPerSegment = 0;

    /// <summary>
    /// Initial prompt
    /// </summary>
    public string Recognition_Whisper_CfgAdv_Prompt
    {
        get => _recognition_Whisper_CfgAdv_Prompt;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_Prompt, value);
    }
    private string _recognition_Whisper_CfgAdv_Prompt = string.Empty;

    /// <summary>
    /// Should thread count be set
    /// </summary>
    public bool Recognition_Whisper_CfgAdv_SetThreads
    {
        get => _recognition_Whisper_CfgAdv_SetThreads;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_SetThreads, value);
    }
    private bool _recognition_Whisper_CfgAdv_SetThreads = false;

    /// <summary>
    /// Use beam search sampling strategy
    /// </summary>
    public bool Recognition_Whisper_CfgAdv_UseBeamSearchSampling
    {
        get => _recognition_Whisper_CfgAdv_UseBeamSearchSampling;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_UseBeamSearchSampling, value);
    }
    private bool _recognition_Whisper_CfgAdv_UseBeamSearchSampling = false;

    /// <summary>
    /// Use greedy sampling strategy
    /// </summary>
    public bool Recognition_Whisper_CfgAdv_UseGreedySampling
    {
        get => _recognition_Whisper_CfgAdv_UseGreedySampling;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_UseGreedySampling, value);
    }
    private bool _recognition_Whisper_CfgAdv_UseGreedySampling = false;

    /// <summary>
    /// Amount of threads used by whisper. 0 = All, -n = All but n threads
    /// </summary>
    public int Recognition_Whisper_CfgAdv_ThreadsUsed
    {
        get => _recognition_Whisper_CfgAdv_ThreadsUsed;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_ThreadsUsed, value.MinMax(int.MinValue, int.MaxValue));
    }
    private int _recognition_Whisper_CfgAdv_ThreadsUsed = -4;
    #endregion

    #region Recognition - Windows
    /// <summary>
    /// Model Id for Windows Recognition
    /// </summary>
    public string Recognition_Windows_ModelId
    {
        get => _recognition_Windows_ModelId;
        set => SetProperty(ref _recognition_Windows_ModelId, value);
    }
    private string _recognition_Windows_ModelId = string.Empty;
    #endregion

    #region Translation - General
    /// <summary>
    /// Current module selected for translation
    /// </summary>
    public string Translation_SelectedModuleName
    {
        get => _translation_SelectedModuleName;
        set => SetProperty(ref _translation_SelectedModuleName, value);
    }
    private string _translation_SelectedModuleName = string.Empty;

    /// <summary>
    /// Autostart for Translation
    /// </summary>
    public bool Translation_AutoStart
    {
        get => _translation_AutoStart;
        set => SetProperty(ref _translation_AutoStart, value);
    }
    private bool _translation_AutoStart = false;

    /// <summary>
    /// Skip longer messages for API Translation, will be cropped otherwise
    /// </summary>
    public bool Translation_SkipLongerMessages
    {
        get => _translation_SkipLongerMessages;
        set => SetProperty(ref _translation_SkipLongerMessages, value);
    }
    private bool _translation_SkipLongerMessages = true;

    /// <summary>
    /// Max Text Length for Translation 
    /// </summary>
    public int Translation_MaxTextLength
    {
        get => _translation_MaxTextLength;
        set => SetProperty(ref _translation_MaxTextLength, value.MinMax(1, short.MaxValue));
    }
    private int _translation_MaxTextLength = 2000;

    /// <summary>
    /// Allow audio output to be translated
    /// </summary>
    public bool Translation_OfAudioOutput //todo: [IMPL] To be implemented
    {
        get => _translation_OfAudioOutput;
        set => SetProperty(ref _translation_OfAudioOutput, value);
    }
    private bool _translation_OfAudioOutput = true;

    /// <summary>
    /// Allow text output to be translated
    /// </summary>
    public bool Translation_OfTextOutput //todo: [IMPL] To be implemented
    {
        get => _translation_OfTextOutput;
        set => SetProperty(ref _translation_OfTextOutput, value);
    }
    private bool _translation_OfTextOutput = true;

    /// <summary>
    /// Allow other output to be translated
    /// </summary>
    public bool Translation_OfOtherOutput //todo: [IMPL] To be implemented
    {
        get => _translation_OfOtherOutput;
        set => SetProperty(ref _translation_OfOtherOutput, value);
    }
    private bool _translation_OfOtherOutput = true;

    /// <summary>
    /// Send untranslated text if nothing can output translation
    /// </summary>
    public bool Translation_SendUntranslatedIfUnavailable
    {
        get => _translation_SendUntranslatedIfUnavailable;
        set => SetProperty(ref _translation_SendUntranslatedIfUnavailable, value);
    }
    private bool _translation_SendUntranslatedIfUnavailable = true;

    /// <summary>
    /// Send untranslated if translation fails
    /// </summary>
    public bool Translation_SendUntranslatedIfFailed
    {
        get => _translation_SendUntranslatedIfFailed;
        set => SetProperty(ref _translation_SendUntranslatedIfFailed, value);
    }
    private bool _translation_SendUntranslatedIfFailed;
    #endregion

    #region Translation - Api
    /// <summary>
    /// API Preset for API Translation 
    /// </summary>
    public string Translation_Api_Preset
    {
        get => _translation_Api_Preset;
        set => SetProperty(ref _translation_Api_Preset, value);
    }
    private string _translation_Api_Preset = string.Empty;
    #endregion

    #region Voice - General
    /// <summary>
    /// Volume of Voice Audio
    /// </summary>
    public int Voice_AudioVolumePercent //todo: [IMPL] To be implemented
    {
        get => _voice_AudioVolumePercent;
        set => SetProperty(ref _voice_AudioVolumePercent, value);
    }
    private int _voice_AudioVolumePercent = 50;

    /// <summary>
    /// Maximum length of text to be converted to voice
    /// </summary>
    public int Voice_MaximumTextLength //todo: [IMPL] To be implemented
    {
        get => _voice_MaximumTextLength;
        set => SetProperty(ref _voice_MaximumTextLength, value.MinMax(1, short.MaxValue));
    }
    private int _voice_MaximumTextLength = 500;

    /// <summary>
    /// Should longer text be skipped? Will be cut otherwise
    /// </summary>
    public bool Voice_SkipLongerText //todo: [IMPL] To be implemented
    {
        get => _voice_SkipLongerText;
        set => SetProperty(ref _voice_SkipLongerText, value);
    }
    private bool _voice_SkipLongerText = true;
    #endregion 

    #region Voice - Azure
    /// <summary>
    /// List of voices to use with Azure TTS
    /// </summary>
    public List<AzureTtsVoiceModel> Voice_Azure_VoiceList //todo: [IMPL] To be implemented
    {
        get => _voice_Azure_Voices;
        set => SetProperty(ref _voice_Azure_Voices, value);
    }
    private List<AzureTtsVoiceModel> _voice_Azure_Voices = [];
    public int Voice_Azure_GetVoiceIndex(string name)
            => Voice_Azure_VoiceList.GetListIndex(x => x.Name == name);

    /// <summary>
    /// Currently selected voice from list
    /// </summary>
    public string Voice_Azure_CurrentVoice //todo: [IMPL] To be implemented
    {
        get => _voice_Azure_CurrentVoice;
        set => SetProperty(ref _voice_Azure_CurrentVoice, value);
    }
    private string _voice_Azure_CurrentVoice = string.Empty;

    /// <summary>
    /// Custom endpoint for Azure Voices
    /// </summary>
    public string Voice_Azure_CustomEndpoint //todo: [IMPL] To be implemented
    {
        get => _voice_Azure_CustomEndpoint;
        set => SetProperty(ref _voice_Azure_CustomEndpoint, value);
    }
    private string _voice_Azure_CustomEndpoint = string.Empty;

    /// <summary>
    /// Override Normal Voice with Azure
    /// </summary>
    public bool Voice_Azure_OverrideNormal //todo: [REFACTOR] Is this needed? Should this not be a module name?
    {
        get => _voice_Azure_OverrideNormal;
        set => SetProperty(ref _voice_Azure_OverrideNormal, value);
    }
    private bool _voice_Azure_OverrideNormal;
    #endregion

    #region Voice - Microsoft
    /// <summary>
    /// ID of Microsoft TTS model
    /// </summary>
    public string Voice_Microsoft_ModelId //todo: [IMPL] To be implemented
    {
        get => _voice_Microsoft_TtsId;
        set => SetProperty(ref _voice_Microsoft_TtsId, value);
    }
    private string _voice_Microsoft_TtsId = string.Empty;
    #endregion
}