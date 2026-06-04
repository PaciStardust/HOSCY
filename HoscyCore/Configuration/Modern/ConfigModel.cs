using CommunityToolkit.Mvvm.ComponentModel;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Serilog.Core;
using Serilog.Events;
using SoundFlow.Extensions.WebRtc.Apm;

namespace HoscyCore.Configuration.Modern;

public class ConfigModel : ObservableObject //todo: [FEAT] Ensure all of this is usable from the CLI
{
    #region !Meta
    public int ConfigVersion { get; set; } = 0;
    #endregion

    #region AFK
    public const string DESC_Afk_ShowDuration = "Periodically display how long the AFK status has been in effect";
    public bool Afk_ShowDuration
    {
        get => _afk_ShowDuration;
        set => SetProperty(ref _afk_ShowDuration, value);
    }
    private bool _afk_ShowDuration = false;

    public const string DESC_Afk_BaseDurationDisplayIntervalSeconds = "The base amount of time (in seconds) between displaying the elapsed AFK duration";
    public float Afk_BaseDurationDisplayIntervalSeconds
    {
        get => _afk_BaseDurationDisplayIntervalSeconds;
        set => SetProperty(ref _afk_BaseDurationDisplayIntervalSeconds, value.MinMax(5, 300));
    }
    private float _afk_BaseDurationDisplayIntervalSeconds = 15f;

    public const string DESC_Afk_TimesDisplayedBeforeDoublingInterval = "How often should the AFK duration be displayed before doubling the time between updates?";
    public int Afk_TimesDisplayedBeforeDoublingInterval
    {
        get => _afk_TimesDisplayedBeforeDoublingInterval;
        set => SetProperty(ref _afk_TimesDisplayedBeforeDoublingInterval, value.MinMax(1, 60));
    }
    private int _afk_TimesDisplayedBeforeDoublingInterval = 12;

    public const string DESC_Afk_StartText = "Text to display when starting AFK";
    public string Afk_StartText
    {
        get => _afk_StartText;
        set => SetProperty(ref _afk_StartText, value.Length > 0 ? value : AFK_NO_STARTTEXT);
    }
    private const string AFK_NO_STARTTEXT = "Now AFK";
    private string _afk_StartText = AFK_NO_STARTTEXT;

    public const string DESC_Afk_StopText = "Text to display when stopping AFK";
    public string Afk_StopText
    {
        get => _afk_EndText;
        set => SetProperty(ref _afk_EndText, value.Length > 0 ? value : AFK_NO_ENDTEXT);
    }
    private const string AFK_NO_ENDTEXT = "No longer AFK";
    private string _afk_EndText = AFK_NO_ENDTEXT;

    public const string DESC_Afk_StatusText = "Text to display as AFK update";
    public string Afk_StatusText
    {
        get => _afk_StatusText;
        set => SetProperty(ref _afk_StatusText, value.Length > 0 ? value : AFK_NO_STATUSTEXT);
    }
    private const string AFK_NO_STATUSTEXT = "AFK since";
    private string _afk_StatusText = AFK_NO_STATUSTEXT;
    #endregion

    #region API
    public const string DESC_Api_Presets = "List of all API presets to be used in various locations";
    public List<ApiPresetModel> Api_Presets
    {
        get => _api_Presets;
        set => SetProperty(ref _api_Presets, value);
    }
    private List<ApiPresetModel> _api_Presets = [];
    public int Api_Presets_GetIndex(string name)
        => Api_Presets.GetListIndex(x => x.Name == name);
    #endregion

    #region Azure
    public const string DESC_AzureServices_Region = "Azure region to be used";
    public string AzureServices_Region
    {
        get => _azureServices_Region;
        set => SetProperty(ref _azureServices_Region, value);
    }
    private string _azureServices_Region = string.Empty;

    public const string DESC_AzureServices_ApiKey = "API Key used to connect to Azure services";
    public string AzureServices_ApiKey
    {
        get => _azureServices_ApiKey;
        set => SetProperty(ref _azureServices_ApiKey, value);
    }
    private string _azureServices_ApiKey = string.Empty;

    public const string DESC_AzureServices_CensorProfanity = "Censor profanity from result";
    public bool AzureServices_CensorProfanity
    {
        get => _azureServices_CensorProfanity;
        set => SetProperty(ref _azureServices_CensorProfanity, value);
    }
    private bool _azureServices_CensorProfanity = false;
    #endregion

    #region Counters
    public const string DESC_Counters_ShowNotification = "Enables notifications to be sent for counter changes";
    public bool Counters_ShowNotification
    {
        get => _counters_ShowNotification;
        set => SetProperty(ref _counters_ShowNotification, value);
    }
    private bool _counters_ShowNotification;

    public const string DESC_Counters_DisplayDurationSeconds = "Duration (in seconds) that a recently triggered counter appears in update notifications";
    public float Counters_DisplayDurationSeconds
    {
        get => _counters_DisplayDurationSeconds;
        set => SetProperty(ref _counters_DisplayDurationSeconds, value.MinMax(0.01f, 30));
    }
    private float _counters_DisplayDurationSeconds = 10f;

    public const string DESC_Counters_DisplayCooldownSeconds = "Duration (in seconds) that notifications are paused after the last send";
    public float Counters_DisplayCooldownSeconds
    {
        get => _counters_DisplayCooldownSeconds;
        set => SetProperty(ref _counters_DisplayCooldownSeconds, value.MinMax(0, 300));
    }
    private float _counters_DisplayCooldownSeconds = 0f;

    public const string DESC_Counters_List = "List of all counters";
    /// </summary>
    public List<CounterModel> Counters_List
    {
        get => _counters_List;
        set => SetProperty(ref _counters_List, value);
    }
    private List<CounterModel> _counters_List = [];
    #endregion

    #region Debug
    public const string DESC_Debug_CheckForUpdatesOnStartup = "Enables update checking and notications";
    public bool Debug_CheckForUpdatesOnStartup //todo: [IMPL++] To be implemented
    {
        get => _debug_CheckForUpdatesOnStartup;
        set => SetProperty(ref _debug_CheckForUpdatesOnStartup, value);
    }
    private bool _debug_CheckForUpdatesOnStartup = true;

    public const string DESC_Debug_LogViaCmdOnWindows = "[WINDOWS ONLY] Opens up CMD/Terminal on launch for logging";
    public bool Debug_LogViaCmdOnWindows
    {
        get => _debug_LogViaCmdOnWindows;
        set => SetProperty(ref _debug_LogViaCmdOnWindows, value);
    }
    private bool _debug_LogViaCmdOnWindows;

    public const string DESC_Debug_LogViaTerminal = "Writes logs to terminal if started in one";
    public bool Debug_LogViaTerminal
    {
        get => _debug_LogViaTerminal;
        set => SetProperty(ref _debug_LogViaTerminal, value);
    }
    private bool _debug_LogViaTerminal;

    public const string DESC_Debug_LogViaFileFollow = "Opens a terminal to follow the log file in";
    public bool Debug_LogViaFileFollow
    {
        get => _debug_LogViaFileFollow;
        set => SetProperty(ref _debug_LogViaFileFollow, value);
    }
    private bool _debug_LogViaFileFollow;

    public const string DESC_Debug_LogFileFollowProcess = "Terminal process (ex. \"foot\") to start for following the log file";
    public string Debug_LogFileFollowProcess
    {
        get => _debug_LogFileFollowProcess;
        set => SetProperty(ref _debug_LogFileFollowProcess, value);
    }
    private string _debug_LogFileFollowProcess = "foot";

    public const string DESC_Debug_LogFileFollowCommand = "Command to execute (ex. \"-e tail -f [LOGFILE]\") to run in terminal process to following the log file";
    public string Debug_LogFileFollowCommand
    {
        get => _debug_LogFileFollowCommand;
        set => SetProperty(ref _debug_LogFileFollowCommand, value);
    }
    private string _debug_LogFileFollowCommand = "-e tail -f [LOGFILE]";

    public const string DESC_Debug_LogMinimumSeverity = "Minimum log severity to be included in the logs";
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

    public const string DESC_Debug_LogFilters = "Filters to apply to logs (for spam avoidance)";
    public List<FilterModel> Debug_LogFilters
    {
        get => _debug_LogFilters;
        set => SetProperty(ref _debug_LogFilters, value);
    }
    private List<FilterModel> _debug_LogFilters = [];

    public const string DESC_Debug_LogVerboseExtra = "Include extra verbose logs for debug purposes";
    public bool Debug_LogVerboseExtra
    {
        get => _debug_LogeVerboseExtra;
        set => SetProperty(ref _debug_LogeVerboseExtra, value);
    }
    private bool _debug_LogeVerboseExtra = false;
    #endregion

    #region External Input
    public const string DESC_ExternalInput_DoPreprocessFull = "Do full preprocessing on external input";
    public bool ExternalInput_DoPreprocessFull 
    {
        get => _externalInput_DoPreprocessFull;
        set => SetProperty(ref _externalInput_DoPreprocessFull, value);
    }
    private bool _externalInput_DoPreprocessFull = true;

    public const string DESC_ExternalInput_DoPreprocessPartial = "Do partial preprocessing on external input";
    public bool ExternalInput_DoPreprocessPartial
    {
        get => _externalInput_DoPreprocessPartial;
        set => SetProperty(ref _externalInput_DoPreprocessPartial, value);
    }
    private bool _externalInput_DoPreprocessPartial = true;

    public const string DESC_ExternalInput_DoTranslate = "Translate external input";
    public bool ExternalInput_DoTranslate
    {
        get => _externalInput_DoTranslate;
        set => SetProperty(ref _externalInput_DoTranslate, value);
    }
    private bool _externalInput_DoTranslate;
    #endregion

    #region Manual Input
    public const string DESC_ManualInput_SendViaAudio = "Send manual input as audio";
    public bool ManualInput_SendViaAudio
    {
        get => _manualInput_SendViaAudio;
        set => SetProperty(ref _manualInput_SendViaAudio, value);
    }
    private bool _manualInput_SendViaAudio;

    public const string DESC_ManualInput_SendViaText = "Send manual input as text";
    public bool ManualInput_SendViaText
    {
        get => _manualInput_SendViaText;
        set => SetProperty(ref _manualInput_SendViaText, value);
    }
    private bool _manualInput_SendViaText = true;

    public const string DESC_ManualInput_SendViaOther = "Send manual input as other";
    public bool ManualInput_SendViaOther
    {
        get => _manualInput_SendViaOther;
        set => SetProperty(ref _manualInput_SendViaOther, value);
    }
    private bool _manualInput_SendViaOther = true;

    public const string DESC_ManualInput_DoPreprocessFull = "Do full preprocessing for manual input";
    public bool ManualInput_DoPreprocessFull
    {
        get => _manualInput_DoPreprocessFull;
        set => SetProperty(ref _manualInput_DoPreprocessFull, value);
    }
    private bool _manualInput_DoPreprocessFull = true;

    public const string DESC_ManualInput_DoPreprocessPartial = "Do partial preprocessing for manual input";
    public bool ManualInput_DoPreprocessPartial
    {
        get => _manualInput_DoPreprocessPartial;
        set => SetProperty(ref _manualInput_DoPreprocessPartial, value);
    }
    private bool _manualInput_DoPreprocessPartial = true;

    public const string DESC_ManualInput_DoTranslate = "Translate manual input";
    public bool ManualInput_DoTranslate
    {
        get => _manualInput_DoTranslate;
        set => SetProperty(ref _manualInput_DoTranslate, value);
    }
    private bool _manualInput_DoTranslate = true;

    public const string DESC_ManualInput_TextPresets = "Presets for manual input";
    public Dictionary<string, string> ManualInput_TextPresets
    {
        get => _manualInput_TextPresets;
        set => SetProperty(ref _manualInput_TextPresets, value);
    }
    private Dictionary<string, string> _manualInput_TextPresets = [];
    #endregion

    #region Media
    public const string DESC_Media_Backend = "Media backend to use for playback control and updates";
    public string Media_Backend
    {
        get => _media_Backend;
        set => SetProperty(ref _media_Backend, value);
    }
    private string _media_Backend = string.Empty;

    public const string DESC_Media_ShowStatus = "Send changes in media as notifications";
    public bool Media_ShowStatus
    {
        get => _media_ShowStatus;
        set => SetProperty(ref _media_ShowStatus, value);
    }
    private bool _media_ShowStatus;

    public const string DESC_Media_PauseText = "Text to display on media pause";
    public string Media_PauseText
    {
        get => _media_PauseText;
        set => SetProperty(ref _media_PauseText, value);
    }
    private string _media_PauseText = "⏸️ Paused";

    public const string DESC_Media_AddAlbumToText = "Add album to media text";
    public bool Media_AddAlbumToText
    {
        get => _media_AddAlbumToText;
        set => SetProperty(ref _media_AddAlbumToText, value);
    }
    private bool _media_AddAlbumToText;

    public const string DESC_Media_FilterSameNameAlbum = "Filter out album from media text if it is similar to the song title";
    public bool Media_FilterSameNameAlbum
    {
        get => _media_FilterSameNameAlbum;
        set => SetProperty(ref _media_FilterSameNameAlbum, value);
    }
    private bool _media_FilterSameNameAlbum = true;

    public const string DESC_Media_SwapArtistAndSongInText = "Swap order of artist name and track title";
    public bool Media_SwapArtistAndSongInText
    {
        get => _media_SwapArtistAndSongInText;
        set => SetProperty(ref _media_SwapArtistAndSongInText, value);
    }
    private bool _media_SwapArtistAndSongInText;

    public const string DESC_Media_PlayingVerb = "Text put in front of media text (ex. \"Playing\")";
    public string Media_PlayingVerb
    {
        get => _media_PlayingVerb;
        set => SetProperty(ref _media_PlayingVerb, value.Length > 0 ? value : NO_MEDIA_PLAYINGVERB);
    }
    private const string NO_MEDIA_PLAYINGVERB = "Playing";
    private string _media_PlayingVerb = NO_MEDIA_PLAYINGVERB;

    public const string DESC_Media_IntermediateWord = "Text put between track title and artist name in media text (ex. \"by\")";
    public string Media_IntermediateWord
    {
        get => _media_IntermediateWord;
        set => SetProperty(ref _media_IntermediateWord, value.Length > 0 ? value : NO_MEDIA_INTERMEDIATEWORD);
    }
    private const string NO_MEDIA_INTERMEDIATEWORD = "by";
    private string _media_IntermediateWord = NO_MEDIA_INTERMEDIATEWORD;

    public const string DESC_Media_AlbumWord = "Text put before album name in media text (ex. \"on\")";
    public string Media_AlbumWord
    {
        get => _media_AlbumWord;
        set => SetProperty(ref _media_AlbumWord, value.Length > 0 ? value : NO_MEDIA_ALBUMWORD);
    }
    private const string NO_MEDIA_ALBUMWORD = "on";
    private string _media_AlbumWord = NO_MEDIA_ALBUMWORD;

    public const string DESC_Media_ExtraText = "Text put after media text";
    public string Media_ExtraText
    {
        get => _media_ExtraText;
        set => SetProperty(ref _media_ExtraText, value);
    }
    private string _media_ExtraText = string.Empty;

    public const string DESC_Media_Filters = "List of words and phrases that will cause media update to not be displayed";
    public List<FilterModel> Media_Filters
    {
        get => _media_Filters;
        set => SetProperty(ref _media_Filters, value);
    }
    private List<FilterModel> _media_Filters = [];
    #endregion

    #region Media - Linux Mpris
    public const string DESC_Media_Mpris_PreferredEndpoints = "List of preferred MPRIS endpoints";
    public List<string> Media_Mpris_PreferredEndpoints
    {
        get => _media_Mpris_PreferredEndpoints;
        set => SetProperty(ref _media_Mpris_PreferredEndpoints, value);
    }
    private List<string> _media_Mpris_PreferredEndpoints = [];

    public const string DESC_Media_Mpris_IgnoredEndpoints = "List of ignored MPRIS endpoints";
    public List<string> Media_Mpris_IgnoredEndpoints
    {
        get => _media_Mpris_IgnoredEndpoints;
        set => SetProperty(ref _media_Mpris_IgnoredEndpoints, value);
    }
    private List<string> _media_Mpris_IgnoredEndpoints = [];

    public const string DESC_Media_Mpris_EndpointUpdateIntervalMs = "Interval (in ms) in which MPRIS endpoint changes are checked";
    public int Media_Mpris_EndpointUpdateIntervalMs 
    {
        get => _media_Mpris_EndpointUpdateIntervalMs;
        set => SetProperty(ref _media_Mpris_EndpointUpdateIntervalMs, value.MinMax(250, 60_000));
    }
    private int _media_Mpris_EndpointUpdateIntervalMs = 1000;
    #endregion

    #region OSC - General
    public const string DESC_Osc_Routing_TargetIp = "Target IP for outbound OSC messages";
    public string Osc_Routing_TargetIp
    {
        get => _osc_Routing_TargetIp;
        set => SetProperty(ref _osc_Routing_TargetIp, value);
    }
    private string _osc_Routing_TargetIp = "127.0.0.1";

    public const string DESC_Osc_Routing_TargetPort = "Target port for outbound OSC messages";
    public ushort Osc_Routing_TargetPort
    {
        get => _osc_Routing_TargetPort;
        set => SetProperty(ref _osc_Routing_TargetPort, value.MinMax(ushort.MinValue, ushort.MaxValue));
    }
    private ushort _osc_Routing_TargetPort = 9000;

    public const string DESC_Osc_Routing_ListenPort = "Port to listen for inbound OSC messages on";
    public int Osc_Routing_ListenPort
    {
        get => _osc_Routing_ListenPort;
        set => SetProperty(ref _osc_Routing_ListenPort, value.MinMax(-1, 65535));
    }
    private int _osc_Routing_ListenPort = 9001;

    public const string DESC_Osc_Relay_Filters = "List of filters for OSC relay";
    public List<OscRelayFilterModel> Osc_Relay_Filters
    {
        get => _osc_Relay_Filters;
        set => SetProperty(ref _osc_Relay_Filters, value);
    }
    private List<OscRelayFilterModel> _osc_Relay_Filters = [];

    public const string DESC_Osc_Relay_IgnoreIfHandled = "Enable OSC message relay for already handled OSC messages";
    public bool Osc_Relay_IgnoreIfHandled
    {
        get => _osc_Relay_IgnoreIfHandled;
        set => SetProperty(ref _osc_Relay_IgnoreIfHandled, value);
    }
    private bool _osc_Relay_IgnoreIfHandled = true;
    #endregion

    #region OSC - Addresses
    public const string DESC_Osc_Address_Tool_ToggleMute = "OSC address to (un)mute recognition when received";
    public string Osc_Address_Tool_ToggleMute
    {
        get => _osc_Address_Tool_ToggleMute;
        set => SetProperty(ref _osc_Address_Tool_ToggleMute, value);
    }
    private string _osc_Address_Tool_ToggleMute = "/avatar/parameters/ToolMute";

    public const string DESC_Osc_Address_Tool_ToggleReplacementsPartial = "OSC address to enable or disable partial replacements";
    public string Osc_Address_Tool_ToggleReplacementsPartial //todo: [IMPL] To be implemented or changed
    {
        get => _osc_Address_Tool_ToggleReplacementsPartial;
        set => SetProperty(ref _osc_Address_Tool_ToggleReplacementsPartial, value);
    }
    private string _osc_Address_Tool_ToggleReplacementsPartial = "/avatar/parameters/ToolToggleReplacementsPartial";

    public const string DESC_Osc_Address_Tool_ToggleReplacementsFull = "OSC address to enable or disable full replacements";
    public string Osc_Address_Tool_ToggleReplacementsFull //todo: [IMPL] To be implemented or changed
    {
        get => _osc_Address_Tool_ToggleReplacementsFull;
        set => SetProperty(ref _osc_Address_Tool_ToggleReplacementsFull, value);
    }
    private string _osc_Address_Tool_ToggleReplacementsFull = "/avatar/parameters/ToolEnableReplacementsFull";

    public const string DESC_Osc_Address_Tool_ToggleRecognitionAutoMute = "OSC address to toggle recognition auto mute";
    public string Osc_Address_Tool_ToggleRecognitionAutoMute
    {
        get => _osc_Address_Tool_ToggleRecognitionAutoMute;
        set => SetProperty(ref _osc_Address_Tool_ToggleRecognitionAutoMute, value);
    }
    private string _osc_Address_Tool_ToggleRecognitionAutoMute = "/avatar/parameters/ToolEnableAutoMute";

    public const string DESC_Osc_Address_Tool_NotificationForRecognitionListening = "OSC address sent out when recognition status changes";
    public string Osc_Address_Tool_NotificationForRecognitionListening //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Tool_NotificationForRecognitionListening;
        set => SetProperty(ref _osc_Address_Tool_NotificationForRecognitionListening, value);
    }
    private string _osc_Address_Tool_NotificationForRecognitionListening = "/avatar/parameters/MicListening";

    public const string DESC_Osc_Address_Game_Mute = "OSC address the game sends out when muted";
    public string Osc_Address_Game_Mute
    {
        get => _osc_Address_Game_Mute;
        set => SetProperty(ref _osc_Address_Game_Mute, value);
    }
    private string _osc_Address_Game_Mute = "/avatar/parameters/MuteSelf";

    public const string DESC_Osc_Address_Game_Afk = "OSC address the game sends out when AFK";
    public string Osc_Address_Game_Afk
    {
        get => _osc_Address_Game_Afk;
        set => SetProperty(ref _osc_Address_Game_Afk, value);
    }
    private string _osc_Address_Game_Afk = "/avatar/parameters/AFK";

    public const string DESC_Osc_Address_Game_Textbox = "OSC address the game listens to for textbox usage";
    public string Osc_Address_Game_Textbox
    {
        get => _osc_Address_Game_Textbox;
        set => SetProperty(ref _osc_Address_Game_Textbox, value);
    }
    private string _osc_Address_Game_Textbox = "/chatbox/input";

    public const string DESC_Osc_Address_Game_Typing = "OSC address the game listens to for typing indicator";
    public string Osc_Address_Game_Typing
    {
        get => _osc_Address_Game_Typing;
        set => SetProperty(ref _osc_Address_Game_Typing, value);
    }
    private string _osc_Address_Game_Typing = "/chatbox/typing";

    public const string DESC_Osc_Address_Input_TextMessage = "OSC address to handle as external message to be sent as text message";
    public string Osc_Address_Input_TextMessage
    {
        get => _osc_Address_Input_TextMessage;
        set => SetProperty(ref _osc_Address_Input_TextMessage, value);
    }
    private string _osc_Address_Input_TextMessage = "/hoscy/message";

    public const string DESC_Osc_Address_Input_TextNotification = "OSC address to handle as external message to be sent as text notification";
    public string Osc_Address_Input_TextNotification
    {
        get => _osc_Address_Input_TextNotification;
        set => SetProperty(ref _osc_Address_Input_TextNotification, value);
    }
    private string _osc_Address_Input_TextNotification = "/hoscy/notification";

    public const string DESC_Osc_Address_Input_AudioMessage = "OSC address to handle as external message to be sent as audio message";
    public string Osc_Address_Input_AudioMessage
    {
        get => _osc_Address_Input_AudioMessage;
        set => SetProperty(ref _osc_Address_Input_AudioMessage, value);
    }
    private string _osc_Address_Input_AudioMessage = "/hoscy/tts";

    public const string DESC_Osc_Address_Input_OtherMessage = "OSC address to handle as external message to be sent as other message";
    public string Osc_Address_Input_OtherMessage
    {
        get => _osc_Address_Input_OtherMessage;
        set => SetProperty(ref _osc_Address_Input_OtherMessage, value);
    }
    private string _osc_Address_Input_OtherMessage = "/hoscy/other";

    public const string DESC_Osc_Address_Media_Pause = "OSC address to handle to pause media";
    public string Osc_Address_Media_Pause
    {
        get => _osc_Address_Media_Pause;
        set => SetProperty(ref _osc_Address_Media_Pause, value);
    }
    private string _osc_Address_Media_Pause = "/avatar/parameters/MediaPause";

    public const string DESC_Osc_Address_Media_Play = "OSC address to handle to resume media";
    public string Osc_Address_Media_Play
    {
        get => _osc_Address_Media_Play;
        set => SetProperty(ref _osc_Address_Media_Play, value);
    }
    private string _osc_Address_Media_Play = "/avatar/parameters/MediaUnpause";

    public const string DESC_Osc_Address_Media_Previous = "OSC address to handle to rewind media";
    public string Osc_Address_Media_Previous
    {
        get => _osc_Address_Media_Previous;
        set => SetProperty(ref _osc_Address_Media_Previous, value);
    }
    private string _osc_Address_Media_Previous = "/avatar/parameters/MediaRewind";

    public const string DESC_Osc_Address_Media_Next = "OSC address to handle to skip media";
    public string Osc_Address_Media_Next
    {
        get => _osc_Address_Media_Next;
        set => SetProperty(ref _osc_Address_Media_Next, value);
    }
    private string _osc_Address_Media_Next = "/avatar/parameters/MediaSkip";

    public const string DESC_Osc_Address_Media_Toggle = "OSC address to handle to toggle media playback";
    public string Osc_Address_Media_Toggle
    {
        get => _osc_Address_Media_Toggle;
        set => SetProperty(ref _osc_Address_Media_Toggle, value);
    }
    private string _osc_Address_Media_Toggle = "/avatar/parameters/MediaToggle";

    public const string DESC_Osc_Address_Tool_Clear = "OSC address to perform a clear";
    public string Osc_Address_Tool_Clear //todo: [IMPL] To be implemented
    {
        get => _osc_Address_Tool_Clear;
        set => SetProperty(ref _osc_Address_Tool_Clear, value);
    }
    private string _osc_Address_Tool_Clear = "/avatar/parameters/ToolClear";
    #endregion

    #region Output - API
    public const string DESC_Output_Api_Enabled = "Enable API output module";
    public bool Output_Api_Enabled //todo: impl
    {
        get => _output_Api_Enabled;
        set => SetProperty(ref _output_Api_Enabled, value);
    }
    private bool _output_Api_Enabled = false;

    public const string DESC_Output_Api_Preset_Message = "API preset for sending messages";
    public string Output_Api_Preset_Message //todo: impl
    {
        get => _output_Api_Preset_Message;
        set => SetProperty(ref _output_Api_Preset_Message, value);
    }
    private string _output_Api_Preset_Message = string.Empty;

    public const string DESC_Output_Api_Preset_Notification = "API preset for sending notifications";
    public string Output_Api_Preset_Notification //todo: impl
    {
        get => _output_Api_Preset_Notification;
        set => SetProperty(ref _output_Api_Preset_Notification, value);
    }
    private string _output_Api_Preset_Notification = string.Empty;

    public const string DESC_Output_Api_Preset_Clear = "API preset for sending clears";
    public string Output_Api_Preset_Clear
    {
        get => _output_Api_Preset_Clear;
        set => SetProperty(ref _output_Api_Preset_Clear, value);
    }
    private string _output_Api_Preset_Clear = string.Empty;

    public const string DESC_Output_Api_Preset_Processing = "API preset for sending processing indicator";
    public string Output_Api_Preset_Processing
    {
        get => _output_Api_Preset_Processing;
        set => SetProperty(ref _output_Api_Preset_Processing, value);
    }
    private string _output_Api_Preset_Processing = string.Empty;

    public const string DESC_Output_Api_Value_True = "Value sent to API as TRUE";
    public string Output_Api_Value_True
    {
        get => _output_Api_Value_True;
        set => SetProperty(ref _output_Api_Value_True, value);
    }
    private string _output_Api_Value_True = string.Empty;

    public const string DESC_Output_Api_Value_False = "Value sent to API as FALSE";
    public string Output_Api_Value_False
    {
        get => _output_Api_Value_False;
        set => SetProperty(ref _output_Api_Value_False, value);
    }
    private string _output_Api_Value_False = string.Empty;

    public const string DESC_Output_Api_TranslationFormat = "Format to use for translations sent to the API";
    public OutputTranslationFormat Output_Api_TranslationFormat
    {
        get => _output_Api_TranslationFormat;
        set => SetProperty(ref _output_Api_TranslationFormat, value);
    }
    private OutputTranslationFormat _output_Api_TranslationFormat = OutputTranslationFormat.Both;
    
    public const string DESC_Output_Api_PrependNotificationPriority = "Prepend notification priority to sent notifications";
    public bool Output_Api_PrependNotificationPriority
    {
        get => _output_Api_PrependNotificationPriority;
        set => SetProperty(ref _output_Api_PrependNotificationPriority, value);
    }
    private bool _output_Api_PrependNotificationPriority = false;
    #endregion

    #region Output - Voice
    public const string DESC_Output_Voice_Enabled = "Enable voice output module";
    public bool Output_Voice_Enabled //todo: impl
    {
        get => _output_Voice_Enabled;
        set => SetProperty(ref _output_Voice_Enabled, value);
    }
    private bool _output_Voice_Enabled = false;

    public const string DESC_Output_Voice_SendTranslated = "Send translationt to voice";
    public bool Output_Voice_SendTranslated //todo: impl
    {
        get => _output_Voice_SendTranslated;
        set => SetProperty(ref _output_Voice_SendTranslated, value);
    }
    private bool _output_Voice_SendTranslated = false;
    #endregion

    #region Output - VRC Textbox
    public const string DESC_Output_VrcTxt_Enabled = "Enable VRC Textbox output module";
    public bool Output_VrcTxt_Enabled
    {
        get => __output_VrcTxt_Enabled;
        set => SetProperty(ref __output_VrcTxt_Enabled, value);
    }
    private bool __output_VrcTxt_Enabled = false;

    public const string DESC_Output_VrcTxt_Send_ShowTranslation = "Send translated content to the Textbox";
    public bool Output_VrcTxt_Send_ShowTranslation
    {
        get => _output_VrcTxt_Send_ShowTranslation;
        set => SetProperty(ref _output_VrcTxt_Send_ShowTranslation, value);
    }
    private bool _output_VrcTxt_Send_ShowTranslation;

    public const string DESC_Output_VrcTxt_Send_AddOriginalToTranslation = "Adds original text after translation";
    public bool Output_VrcTxt_Send_AddOriginalToTranslation
    {
        get => _output_VrcTxt_Send_AddOriginalToTranslation;
        set => SetProperty(ref _output_VrcTxt_Send_AddOriginalToTranslation, value);
    }
    private bool _output_VrcTxt_Send_AddOriginalToTranslation = true;

    public const string DESC_Output_VrcTxt_Send_MaxDisplayedCharacters = "Maximum characters to be displayed in Textbox at once";
    public int Output_VrcTxt_Send_MaxDisplayedCharacters
    {
        get => _output_VrcTxt_Send_MaxDisplayedCharacters;
        set => SetProperty(ref _output_VrcTxt_Send_MaxDisplayedCharacters, value.MinMax(10, 130));
    }
    private int _output_VrcTxt_Send_MaxDisplayedCharacters = 130;

    public const string DESC_Output_VrcTxt_Do_Send = "Enable output of text (disable to only have processing indicator)";
    public bool Output_VrcTxt_Do_Send
    {
        get => _output_VrcTxt_Do_Output; 
        set => SetProperty(ref _output_VrcTxt_Do_Output, value);
    }
    private bool _output_VrcTxt_Do_Output = true;

    public const string DESC_Output_VrcTxt_Do_Indicator = "Use the processing indicator of the Textbox";
    public bool Output_VrcTxt_Do_Indicator
    {
        get => _output_VrcTxt_Do_Indicator;
        set => SetProperty(ref _output_VrcTxt_Do_Indicator, value);
    }
    private bool _output_VrcTxt_Do_Indicator = true;

    public const string DESC_Output_VrcTxt_Timeout_DynamicPer20CharactersDisplayedMs = "Timeout (in ms) per 20 characters to be displayed at once";
    public int Output_VrcTxt_Timeout_DynamicPer20CharactersDisplayedMs
    {
        get => _output_VrcTxt_Timeout_DynamicPer20CharactersDisplayedMs;
        set => SetProperty(ref _output_VrcTxt_Timeout_DynamicPer20CharactersDisplayedMs, value.MinMax(250, 10000));
    }
    private int _output_VrcTxt_Timeout_DynamicPer20CharactersDisplayedMs = 1250;

    public const string DESC_Output_VrcTxt_Timeout_DynamicMinimumMs = "Minimum timeout (in ms) when computing timeout per 20 characters";
    public int Output_VrcTxt_Timeout_DynamicMinimumMs
    {
        get => _output_VrcTxt_Timeout_DynamicMinimumMs;
        set => SetProperty(ref _output_VrcTxt_Timeout_DynamicMinimumMs, value.MinMax(1250, 30000));
    }
    private int _output_VrcTxt_Timeout_DynamicMinimumMs = 3000;

    public const string DESC_Output_VrcTxt_Timeout_StaticMs = "Static timeout (in ms) for sent text";
    public int Output_VrcTxt_Timeout_StaticMs
    {
        get => _output_VrcTxt_Timeout_StaticMs;
        set => SetProperty(ref _output_VrcTxt_Timeout_StaticMs, value.MinMax(1250, 30000));
    }
    private int _output_VrcTxt_Timeout_StaticMs = 5000;

    public const string DESC_Output_VrcTxt_Timeout_UseDynamic = "Use dynamically calculated timeout for sent text";
    public bool Output_VrcTxt_Timeout_UseDynamic
    {
        get => _output_VrcTxt_Timeout_UseDynamic;
        set => SetProperty(ref _output_VrcTxt_Timeout_UseDynamic, value);
    }
    private bool _output_VrcTxt_Timeout_UseDynamic = true;

    public const string DESC_Output_VrcTxt_Timeout_AutomaticallyClearNotification = "Perform an automatic clear after notification timeout";
    public bool Output_VrcTxt_Timeout_AutomaticallyClearNotification
    {
        get => _output_VrcTxt_Timeout_AutomaticallyClearNotification;
        set => SetProperty(ref _output_VrcTxt_Timeout_AutomaticallyClearNotification, value);
    }
    private bool _output_VrcTxt_Timeout_AutomaticallyClearNotification = true;

    public const string DESC_Output_VrcTxt_Timeout_AutomaticallyClearMessage = "Perform an automatic clear after message timeout";
    public bool Output_VrcTxt_Timeout_AutomaticallyClearMessage
    {
        get => _output_VrcTxt_Timeout_AutomaticallyClearMessage;
        set => SetProperty(ref _output_VrcTxt_Timeout_AutomaticallyClearMessage, value);
    }
    private bool _output_VrcTxt_Timeout_AutomaticallyClearMessage;

    public const string DESC_Output_VrcTxt_Notification_IndicatorTextStart = "Text to add at the start of the sent notification";
    public string Output_VrcTxt_Notification_IndicatorTextStart
    {
        get => _output_VrcTxt_Notification_IndicatorTextStart;
        set => SetProperty(ref _output_VrcTxt_Notification_IndicatorTextStart, value.Length < 4 ? value : value[..3]);
    }
    private string _output_VrcTxt_Notification_IndicatorTextStart = "〈";
    
    public const string DESC_Output_VrcTxt_Notification_IndicatorTextEnd = "Text to add at the end of the sent notification";
    public string Output_VrcTxt_Notification_IndicatorTextEnd
    {
        get => _output_VrcTxt_Notification_IndicatorTextEnd;
        set => SetProperty(ref _output_VrcTxt_Notification_IndicatorTextEnd, value.Length < 4 ? value : value[..3]);
    }
    private string _output_VrcTxt_Notification_IndicatorTextEnd = "〉";

    public const string DESC_Output_VrcTxt_Notification_UsePrioritySystem = "Use priority system for notifications";
    public bool Output_VrcTxt_Notification_UsePrioritySystem
    {
        get => _output_VrcTxt_Notification_UsePrioritySystem;
        set => SetProperty(ref _output_VrcTxt_Notification_UsePrioritySystem, value);
    }
    private bool _output_VrcTxt_Notification_UsePrioritySystem = true;

    public const string DESC_Output_VrcTxt_Notification_SkipWhenMessageAvailable = "Skip notifications when there is an available message";
    public bool Output_VrcTxt_Notification_SkipWhenMessageAvailable
    {
        get => _output_VrcTxt_Notification_SkipWhenMessageAvailable;
        set => SetProperty(ref _output_VrcTxt_Notification_SkipWhenMessageAvailable, value);
    }
    private bool _output_VrcTxt_Notification_SkipWhenMessageAvailable = true;

    public const string DESC_Output_VrcTxt_Sound_OnMessage = "Play textbox sound on message";
    public bool Output_VrcTxt_Sound_OnMessage
    {
        get => _output_VrcTxt_Sound_OnMessage;
        set => SetProperty(ref _output_VrcTxt_Sound_OnMessage, value);
    }
    private bool _output_VrcTxt_Sound_OnMessage = true;

    public const string DESC_Output_VrcTxt_Sound_OnNotification = "Play textbox sound on notification";
    public bool Output_VrcTxt_Sound_OnNotification
    {
        get => _output_VrcTxt_Sound_OnNotification;
        set => SetProperty(ref _output_VrcTxt_Sound_OnNotification, value);
    }
    private bool _output_VrcTxt_Sound_OnNotification;
    #endregion

    #region Preprocessing
    public const string DESC_Preprocessing_DoReplacementsPartial = "Enables/Disables partial replacements entirely";
    public bool Preprocessing_DoReplacementsPartial
    {
        get => _preprocessing_DoReplacementsPartial;
        set => SetProperty(ref _preprocessing_DoReplacementsPartial, value);
    }
    private bool _preprocessing_DoReplacementsPartial = true;

    public const string DESC_Preprocessing_DoReplacementsFull = "Enables/Disables full replacements entirely";
    public bool Preprocessing_DoReplacementsFull
    {
        get => _preprocessing_DoReplacementsFull;
        set => SetProperty(ref _preprocessing_DoReplacementsFull, value);
    }
    private bool _preprocessing_DoReplacementsFull = true;

    public const string DESC_Preprocessing_ReplacementsFull = "List of full replacements to apply";
    public List<ReplacementDataModel> Preprocessing_ReplacementsFull
    {
        get => _preprocessing_ReplacementsFull;
        set => SetProperty(ref _preprocessing_ReplacementsFull, value);
    }
    private List<ReplacementDataModel> _preprocessing_ReplacementsFull = [];

    public const string DESC_Preprocessing_ReplacementsPartial = "List of partial replacements to apply";
    public List<ReplacementDataModel> Preprocessing_ReplacementsPartial
    {
        get => _preprocessing_ReplacementsPartial;
        set => SetProperty(ref _preprocessing_ReplacementsPartial, value);
    }
    private List<ReplacementDataModel> _preprocessing_ReplacementsPartial = [];

    public const string DESC_Preprocessing_ReplacementFullIgnoredCharacters = "Characters that get ignored for full replacements";
    public string Preprocessing_ReplacementFullIgnoredCharacters
    {
        get => _preprocessing_ReplacementFullIgnoredCharacters;
        set => SetProperty(ref _preprocessing_ReplacementFullIgnoredCharacters, value);
    }
    private string _preprocessing_ReplacementFullIgnoredCharacters = ".?!,。、！？";
    #endregion

    #region Recognition - General
    public const string DESC_Recognition_MicrophoneName = "Microphone to use for recognition";
    public string Recognition_MicrophoneName
    {
        get => _recognition_MicrophoneName;
        set => SetProperty(ref _recognition_MicrophoneName, value);
    }
    private string _recognition_MicrophoneName = string.Empty;

    public const string DESC_Recognition_Send_ViaText = "Send recognition result over text";
    public bool Recognition_Send_ViaText
    {
        get => _recognition_Send_ViaText;
        set => SetProperty(ref _recognition_Send_ViaText, value);
    }
    private bool _recognition_Send_ViaText = true;

    public const string DESC_Recognition_Send_ViaAudio = "Send recognition result over audio";
    public bool Recognition_Send_ViaAudio
    {
        get => _recognition_Send_ViaAudio;
        set => SetProperty(ref _recognition_Send_ViaAudio, value);
    }
    private bool _recognition_Send_ViaAudio = false;

    public const string DESC_Recognition_Send_ViaOther = "Send recognition result over other";
    public bool Recognition_Send_ViaOther
    {
        get => _recognition_Send_ViaOther;
        set => SetProperty(ref _recognition_Send_ViaOther, value);
    }
    private bool _recognition_Send_ViaOther = false;

    public const string DESC_Recognition_Send_DoTranslate = "Translate recognition result";
    public bool Recognition_Send_DoTranslate
    {
        get => _recognition_Send_DoTranslate;
        set => SetProperty(ref _recognition_Send_DoTranslate, value);
    }
    private bool _recognition_Send_DoTranslate = false;

    public const string DESC_Recognition_Send_DoPreprocessPartial = "Apply partial preprocessing of recognition result";
    public bool Recognition_Send_DoPreprocessPartial
    {
        get => _recognition_Send_DoPreprocessPartial;
        set => SetProperty(ref _recognition_Send_DoPreprocessPartial, value);
    }
    private bool _recognition_Send_DoPreprocessPartial = true;

    public const string DESC_Recognition_Send_DoPreprocessFull = "Apply full preprocessing of recognition result";
    public bool Recognition_Send_DoPreprocessFull
    {
        get => _recognition_Send_DoPreprocessFull;
        set => SetProperty(ref _recognition_Send_DoPreprocessFull, value);
    }
    private bool _recognition_Send_DoPreprocessFull = true;

    public const string DESC_Recognition_Mute_StartUnmuted = "Unmute recognition on startup";
    public bool Recognition_Mute_StartUnmuted
    {
        get => _recognition_Mute_StartUnmuted;
        set => SetProperty(ref _recognition_Mute_StartUnmuted, value);
    }
    private bool _recognition_Mute_StartUnmuted = true;

    public const string DESC_Recognition_Mute_PlaySound = "Play a sound on mute/unmute";
    public bool Recognition_Mute_PlaySound //todo: [IMPL] To be implemented
    {
        get => _recognition_Mute_PlaySound;
        set => SetProperty(ref _recognition_Mute_PlaySound, value);
    }
    private bool _recognition_Mute_PlaySound = true;

    public const string DESC_Recognition_Mute_OnGameMute = "Mute and unmute recognition when receiving mute signal via OSC";
    public bool Recognition_Mute_OnGameMute
    {
        get => _recognition_Mute_OnGameMute;
        set => SetProperty(ref _recognition_Mute_OnGameMute, value);
    }
    private bool _recognition_Mute_OnGameMute = true;

    public const string DESC_Recognition_SelectedModuleName = "Module used for recognition";
    public string Recognition_SelectedModuleName
    {
        get => _recognition_SelectedModuleName;
        set => SetProperty(ref _recognition_SelectedModuleName, value);
    }
    private string _recognition_SelectedModuleName = string.Empty;

    public const string DESC_Recognition_AutoStart = "Start recognition on launch";
    public bool Recognition_AutoStart
    {
        get => _recognition_AutoStart;
        set => SetProperty(ref _recognition_AutoStart, value);
    }
    private bool _recognition_AutoStart = false;

    public const string DESC_Recognition_Fixup_NoiseFilter = "List of noises to be removed from output";
    public HashSet<string> Recognition_Fixup_NoiseFilter
    {
        get => _recognition_Fixup_NoiseFilter;
        set => SetProperty(ref _recognition_Fixup_NoiseFilter, value);
    }
    private HashSet<string> _recognition_Fixup_NoiseFilter = [];

    public const string DESC_Recognition_Fixup_RemoveEndPeriod = "Remove the period at the end of a message";
    public bool Recognition_Fixup_RemoveEndPeriod
    {
        get => _recognition_Fixup_RemoveEndPeriod;
        set => SetProperty(ref _recognition_Fixup_RemoveEndPeriod, value);
    }
    private bool _recognition_Fixup_RemoveEndPeriod = true;

    public const string DESC_Recognition_Fixup_CapitalizeFirstLetter = "Capitalizes the first character of the message";
    public bool Recognition_Fixup_CapitalizeFirstLetter
    {
        get => _recognition_Fixup_CapitalizeFirstLetter;
        set => SetProperty(ref _recognition_Fixup_CapitalizeFirstLetter, value);
    }
    private bool _recognition_Fixup_CapitalizeFirstLetter = true;
    #endregion

    #region Recognition - API
    public const string DESC_Recognition_Api_Preset = "API preset for API speech recognition";
    public string Recognition_Api_Preset
    {
        get => _recognition_Api_Preset;
        set => SetProperty(ref _recognition_Api_Preset, value);
    }
    private string _recognition_Api_Preset = string.Empty;

    public const string DESC_Recognition_Api_MaxRecordingTime = "Maximum recording time (in seconds) at a time";
    public int Recognition_Api_MaxRecordingTime
    {
        get => _recognition_Api_MaxRecordingTime;
        set => SetProperty(ref _recognition_Api_MaxRecordingTime, value.MinMax( 1, 300));
    }
    private int _recognition_Api_MaxRecordingTime = 30;
    #endregion

    #region Recognition - Azure
    public const string DESC_Recognition_Azure_CustomEndpoint = "Custom endpoint for Azure speech recognition";
    public string Recognition_Azure_CustomEndpoint
    {
        get => _recognition_Azure_CustomEndpoint;
        set => SetProperty(ref _recognition_Azure_CustomEndpoint, value);
    }
    private string _recognition_Azure_CustomEndpoint = string.Empty;

    public const string DESC_Recognition_Azure_PresetPhrases = "Preset phrases to use";
    public HashSet<string> Recognition_Azure_PresetPhrases
    {
        get => _recognition_Azure_Phrases;
        set => SetProperty(ref _recognition_Azure_Phrases, value);
    }
    private HashSet<string> _recognition_Azure_Phrases = [];

    public const string DESC_Recognition_Azure_Languages = "Valid languages for speech recognition";
    public HashSet<string> Recognition_Azure_Languages
    {
        get => _recognition_Azure_Languages;
        set => SetProperty(ref _recognition_Azure_Languages, value);
    }
    private HashSet<string> _recognition_Azure_Languages = [];
    #endregion

    #region Recognition - Vosk
    public const string DESC_Recognition_Vosk_Models = "List of available vosk models with file path";
    public Dictionary<string, string> Recognition_Vosk_Models
    {
        get => _recognition_Vosk_Models;
        set => SetProperty(ref _recognition_Vosk_Models, value);
    }
    private Dictionary<string, string> _recognition_Vosk_Models = [];

    public const string DESC_Recognition_Vosk_CurrentModel = "Vosk model from list to use";
    public string Recognition_Vosk_CurrentModel
    {
        get => _recognition_Vosk_CurrentModel;
        set => SetProperty(ref _recognition_Vosk_CurrentModel, value);
    }
    private string _recognition_Vosk_CurrentModel = string.Empty;

    public const string DESC_Recognition_Vosk_NewWordWaitTimeMs = "Time to wait (in ms) for new word before stopping sentence";
    public int Recognition_Vosk_NewWordWaitTimeMs
    {
        get => _recognition_Vosk_NewWordWaitTimeMs;
        set => SetProperty(ref _recognition_Vosk_NewWordWaitTimeMs,value.MinMax(500, 30000));
    }
    private int _recognition_Vosk_NewWordWaitTimeMs = 2500;
    #endregion

    #region Recognition - Whisper
    public const string DESC_Recognition_Whisper_Models = "List of whisper models with file path";
    public Dictionary<string, string> Recognition_Whisper_Models
    {
        get => _recognition_Whisper_Models;
        set => SetProperty(ref _recognition_Whisper_Models, value);
    }
    private Dictionary<string, string> _recognition_Whisper_Models = [];

    public const string DESC_Recognition_Whisper_SelectedModel = "Whisper model to use";
    public string Recognition_Whisper_SelectedModel
    {
        get => _recognition_Whisper_SelectedModel;
        set => SetProperty(ref _recognition_Whisper_SelectedModel, value);
    }
    private string _recognition_Whisper_SelectedModel = string.Empty;

    public const string DESC_Recognition_Whisper_Fix_RemoveRandomBrackets = "Fix random brackets in the output \"('( ( (')\"";
    public bool Recognition_Whisper_Fix_RemoveRandomBrackets
    {
        get => _recognition_Whisper_Fix_RemoveRandomBrackets;
        set => SetProperty(ref _recognition_Whisper_Fix_RemoveRandomBrackets, value);
    }
    private bool _recognition_Whisper_Fix_RemoveRandomBrackets = true;

    public const string DESC_Recognition_Whisper_Dbg_LogFilteredNoises = "Write noises that have been filtered out to the logs";
    public bool Recognition_Whisper_Dbg_LogFilteredNoises
    {
        get => _recognition_Whisper_Dbg_LogFilteredNoises;
        set => SetProperty(ref _recognition_Whisper_Dbg_LogFilteredNoises, value);
    }
    private bool _recognition_Whisper_Dbg_LogFilteredNoises = false;

    public const string DESC_Recognition_Whisper_Cfg_NoiseFilter = "List of allowed whisper noises";
    public Dictionary<string, string> Recognition_Whisper_Cfg_NoiseFilter
    {
        get => _recognition_Whisper_Cfg_NoiseFilter;
        set => SetProperty(ref _recognition_Whisper_Cfg_NoiseFilter, value);
    }
    private Dictionary<string, string> _recognition_Whisper_Cfg_NoiseFilter = [];

    public const string DESC_Recognition_Whisper_Cfg_UseSingleSegmentMode = "Use single segment mode for higher accuracy but reduced functionality";
    public bool Recognition_Whisper_Cfg_UseSingleSegmentMode
    {
        get => _recognition_Whisper_Cfg_UseSingleSegmentMode;
        set => SetProperty(ref _recognition_Whisper_Cfg_UseSingleSegmentMode, value);
    }
    private bool _recognition_Whisper_Cfg_UseSingleSegmentMode = true;

    public const string DESC_Recognition_Whisper_Cfg_TranslateToEnglish = "Translate to English if the detected language is not English";
    public bool Recognition_Whisper_Cfg_TranslateToEnglish
    {
        get => _recognition_Whisper_Cfg_TranslateToEnglish;
        set => SetProperty(ref _recognition_Whisper_Cfg_TranslateToEnglish, value);
    }
    private bool _recognition_Whisper_Cfg_TranslateToEnglish = false;

    public const string DESC_Recognition_Whisper_Cfg_UseGpu = "Use the GPU for whisper";
    public bool Recognition_Whisper_Cfg_UseGpu
    {
        get => _recognition_Whisper_Cfg_UseGpu;
        set => SetProperty(ref _recognition_Whisper_Cfg_UseGpu, value);
    }
    private bool _recognition_Whisper_Cfg_UseGpu = true;

    public const string DESC_Recognition_Whisper_Cfg_DetectLanguage = "Detect language automatically";
    public bool Recognition_Whisper_Cfg_DetectLanguage
    {
        get => _recognition_Whisper_Cfg_DetectLanguage;
        set => SetProperty(ref _recognition_Whisper_Cfg_DetectLanguage, value);
    }
    private bool _recognition_Whisper_Cfg_DetectLanguage = false;

    public const string DESC_Recognition_Whisper_Cfg_Language = "Shortcode for used language (empty = auto)";
    public string Recognition_Whisper_Cfg_Language
    {
        get => _recognition_Whisper_Cfg_Language;
        set => SetProperty(ref _recognition_Whisper_Cfg_Language, value);
    }
    private string _recognition_Whisper_Cfg_Language = string.Empty;

    public const string DESC_Recognition_Whisper_Cfg_MaxSentenceDurationMs = "Rough maximum cutoff time (in ms) for sentences";
    public int Recognition_Whisper_Cfg_MaxSentenceDurationMs
    {
        get => _recognition_Whisper_Cfg_MaxSentenceDurationMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_MaxSentenceDurationMs, value.MinMax(4_000, int.MaxValue));
    }
    private int _recognition_Whisper_Cfg_MaxSentenceDurationMs = 16_000;

    public const string DESC_Recognition_Whisper_Cfg_MinSentenceDurationMs = "Minimum time (in ms) for sentences";
    public int Recognition_Whisper_Cfg_MinSentenceDurationMs
    {
        get => _recognition_Whisper_Cfg_MinSentenceDurationMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_MinSentenceDurationMs, value.MinMax(100, 2_000));
    }
    private int _recognition_Whisper_Cfg_MinSentenceDurationMs = 250;

    public const string DESC_Recognition_Whisper_Cfg_DetectPauseDurationMs = "Duration (in ms) of a pause for cutoff in";
    public int Recognition_Whisper_Cfg_DetectPauseDurationMs
    {
        get => _recognition_Whisper_Cfg_DetectPauseDurationMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_DetectPauseDurationMs, value.MinMax(250, 2_000));
    }
    private int _recognition_Whisper_Cfg_DetectPauseDurationMs = 500;

    public const string DESC_Recognition_Whisper_Cfg_DetectOuterSilenceDurationMs = "Duration (in ms) to recognize a silence in outer segments";
    public int Recognition_Whisper_Cfg_DetectOuterSilenceDurationMs
    {
        get => _recognition_Whisper_Cfg_DetectOuterSilenceDurationMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_DetectOuterSilenceDurationMs, value.MinMax(100, 1000));
    }
    private int _recognition_Whisper_Cfg_DetectOuterSilenceDurationMs = 250;

    public const string DESC_Recognition_Whisper_Cfg_RecognitionUpdateIntervalMs = "Update rate (in ms) for intermediate processing (lower = more intensive)";
    public int Recognition_Whisper_Cfg_RecognitionUpdateIntervalMs
    {
        get => _recognition_Whisper_Cfg_RecognitionUpdateIntervalMs;
        set => SetProperty(ref _recognition_Whisper_Cfg_RecognitionUpdateIntervalMs, value.MinMax(250, 4_000));
    }
    private int _recognition_Whisper_Cfg_RecognitionUpdateIntervalMs = 500;

    public const string DESC_Recognition_Whisper_Cfg_VadOperatingMode = "Operating mode for voice activity detection";
    public WhisperIpcVadOperatingMode Recognition_Whisper_Cfg_VadOperatingMode
    {
        get => _recognition_Whisper_Cfg_VadOperatingMode;
        set => SetProperty(ref _recognition_Whisper_Cfg_VadOperatingMode, value);
    }
    private WhisperIpcVadOperatingMode _recognition_Whisper_Cfg_VadOperatingMode = WhisperIpcVadOperatingMode.Aggressive;

    public const string DESC_Recognition_Whisper_CfgAdv_BeamSize = "Beam size for beam search sampling strategy";
    public int Recognition_Whisper_CfgAdv_BeamSize
    {
        get => _recognition_Whisper_CfgAdv_BeamSize;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_BeamSize, value.MinMax(0, 10));
    }
    private int _recognition_Whisper_CfgAdv_BeamSize = 0;

    public const string DESC_Recognition_Whisper_CfgAdv_GreedyBestOf = "Best of for greedy sampling strategy";
    public int Recognition_Whisper_CfgAdv_GreedyBestOf
    {
        get => _recognition_Whisper_CfgAdv_GreedyBestOf;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_GreedyBestOf, value.MinMax(0, 10));
    }
    private int _recognition_Whisper_CfgAdv_GreedyBestOf = 0;

    public const string DESC_Recognition_Whisper_CfgAdv_GraphicsAdapterId = "Id of GPU to use";
    public int Recognition_Whisper_CfgAdv_GraphicsAdapterId
    {
        get => _recognition_Whisper_CfgAdv_GraphicsAdapterId;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_GraphicsAdapterId, value.MinMax(0, int.MaxValue));
    }
    private int _recognition_Whisper_CfgAdv_GraphicsAdapterId = 0;

    public const string DESC_Recognition_Whisper_CfgAdv_MaxInitialT = "MaxInitialT for Whisper";
    public float Recognition_Whisper_CfgAdv_MaxInitialT
    {
        get => _recognition_Whisper_CfgAdv_MaxInitialT;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_MaxInitialT, value.MinMax(-1, 1));
    }
    private float _recognition_Whisper_CfgAdv_MaxInitialT = -1;

    public const string DESC_Recognition_Whisper_CfgAdv_NoSpeechThreshold = "No speech threshold for Whisper";
    public float Recognition_Whisper_CfgAdv_NoSpeechThreshold
    {
        get => _recognition_Whisper_CfgAdv_NoSpeechThreshold;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_NoSpeechThreshold, value.MinMax(-1, 1));
    }
    private float _recognition_Whisper_CfgAdv_NoSpeechThreshold = -1;

    public const string DESC_Recognition_Whisper_CfgAdv_Temperature = "Temperature for Whisper";
    public float Recognition_Whisper_CfgAdv_Temperature
    {
        get => _recognition_Whisper_CfgAdv_Temperature;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_Temperature, value.MinMax(-1, 1));
    }
    private float _recognition_Whisper_CfgAdv_Temperature = -1;

    public const string DESC_Recognition_Whisper_CfgAdv_TemperatureInc = "TemperatureInc for Whisper";
    public float Recognition_Whisper_CfgAdv_TemperatureInc
    {
        get => _recognition_Whisper_CfgAdv_TemperatureInc;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_TemperatureInc, value.MinMax(-1, 1));
    }
    private float _recognition_Whisper_CfgAdv_TemperatureInc = -1;

    public const string DESC_Recognition_Whisper_CfgAdv_MaxSegmentLength = "Maximum segment length";
    public int Recognition_Whisper_CfgAdv_MaxSegmentLength
    {
        get => _recognition_Whisper_CfgAdv_MaxSegmentLength;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_MaxSegmentLength, value.MinMax(0, int.MaxValue));
    }
    private int _recognition_Whisper_CfgAdv_MaxSegmentLength = 0;

    public const string DESC_Recognition_Whisper_CfgAdv_MaxTokensPerSegment = "Maxiumum amount of tokens per segment";
    public int Recognition_Whisper_CfgAdv_MaxTokensPerSegment
    {
        get => _recognition_Whisper_CfgAdv_MaxTokensPerSegment;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_MaxTokensPerSegment, value.MinMax(0, int.MaxValue));
    }
    private int _recognition_Whisper_CfgAdv_MaxTokensPerSegment = 0;

    public const string DESC_Recognition_Whisper_CfgAdv_Prompt = "Initial prompt for whisper";
    public string Recognition_Whisper_CfgAdv_Prompt
    {
        get => _recognition_Whisper_CfgAdv_Prompt;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_Prompt, value);
    }
    private string _recognition_Whisper_CfgAdv_Prompt = string.Empty;

    public const string DESC_Recognition_Whisper_CfgAdv_SetThreads = "Enable setting thread count";
    public bool Recognition_Whisper_CfgAdv_SetThreads
    {
        get => _recognition_Whisper_CfgAdv_SetThreads;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_SetThreads, value);
    }
    private bool _recognition_Whisper_CfgAdv_SetThreads = false;

    public const string DESC_Recognition_Whisper_CfgAdv_UseBeamSearchSampling = "Enable beam search sampling strategy";
    public bool Recognition_Whisper_CfgAdv_UseBeamSearchSampling
    {
        get => _recognition_Whisper_CfgAdv_UseBeamSearchSampling;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_UseBeamSearchSampling, value);
    }
    private bool _recognition_Whisper_CfgAdv_UseBeamSearchSampling = false;

    public const string DESC_Recognition_Whisper_CfgAdv_UseGreedySampling = "Enable greedy sampling strategy";
    public bool Recognition_Whisper_CfgAdv_UseGreedySampling
    {
        get => _recognition_Whisper_CfgAdv_UseGreedySampling;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_UseGreedySampling, value);
    }
    private bool _recognition_Whisper_CfgAdv_UseGreedySampling = false;

    public const string DESC_Recognition_Whisper_CfgAdv_ThreadsUsed = "Amount of threads used by whisper (0 = all, -n = all but n threads)";
    public int Recognition_Whisper_CfgAdv_ThreadsUsed
    {
        get => _recognition_Whisper_CfgAdv_ThreadsUsed;
        set => SetProperty(ref _recognition_Whisper_CfgAdv_ThreadsUsed, value.MinMax(int.MinValue, int.MaxValue));
    }
    private int _recognition_Whisper_CfgAdv_ThreadsUsed = -4;
    #endregion

    #region Recognition - Windows
    public const string DESC_Recognition_Windows_ModelId = "Model Id for Windows recognition";
    public string Recognition_Windows_ModelId
    {
        get => _recognition_Windows_ModelId;
        set => SetProperty(ref _recognition_Windows_ModelId, value);
    }
    private string _recognition_Windows_ModelId = string.Empty;
    #endregion

    #region Translation - General
    public const string DESC_Translation_SelectedModuleName = "Current module selected for translation";
    public string Translation_SelectedModuleName
    {
        get => _translation_SelectedModuleName;
        set => SetProperty(ref _translation_SelectedModuleName, value);
    }
    private string _translation_SelectedModuleName = string.Empty;

    public const string DESC_Translation_AutoStart = "Start translation module on launch";
    public bool Translation_AutoStart
    {
        get => _translation_AutoStart;
        set => SetProperty(ref _translation_AutoStart, value);
    }
    private bool _translation_AutoStart = false;

    public const string DESC_Translation_SkipLongerMessages = "Skip longer messages for API Translation, will be cropped otherwise";
    public bool Translation_SkipLongerMessages
    {
        get => _translation_SkipLongerMessages;
        set => SetProperty(ref _translation_SkipLongerMessages, value);
    }
    private bool _translation_SkipLongerMessages = true;

    public const string DESC_Translation_MaxTextLength = "Maximum text length for translation";
    public int Translation_MaxTextLength
    {
        get => _translation_MaxTextLength;
        set => SetProperty(ref _translation_MaxTextLength, value.MinMax(1, short.MaxValue));
    }
    private int _translation_MaxTextLength = 2000;

    public const string DESC_Translation_OfAudioOutput = "Allow audio output to be translated";
    public bool Translation_OfAudioOutput //todo: [IMPL] To be implemented
    {
        get => _translation_OfAudioOutput;
        set => SetProperty(ref _translation_OfAudioOutput, value);
    }
    private bool _translation_OfAudioOutput = true;

    public const string DESC_Translation_OfTextOutput = "Allow text output to be translated";
    public bool Translation_OfTextOutput //todo: [IMPL] To be implemented
    {
        get => _translation_OfTextOutput;
        set => SetProperty(ref _translation_OfTextOutput, value);
    }
    private bool _translation_OfTextOutput = true;

    public const string DESC_Translation_OfOtherOutput = "Allow other output to be translated";
    public bool Translation_OfOtherOutput //todo: [IMPL] To be implemented
    {
        get => _translation_OfOtherOutput;
        set => SetProperty(ref _translation_OfOtherOutput, value);
    }
    private bool _translation_OfOtherOutput = true;

    public const string DESC_Translation_SendUntranslatedIfUnavailable = "Send untranslated text if nothing can output translation";
    public bool Translation_SendUntranslatedIfUnavailable
    {
        get => _translation_SendUntranslatedIfUnavailable;
        set => SetProperty(ref _translation_SendUntranslatedIfUnavailable, value);
    }
    private bool _translation_SendUntranslatedIfUnavailable = true;

    public const string DESC_Translation_SendUntranslatedIfFailed = "Send untranslated if translation fails";
    public bool Translation_SendUntranslatedIfFailed
    {
        get => _translation_SendUntranslatedIfFailed;
        set => SetProperty(ref _translation_SendUntranslatedIfFailed, value);
    }
    private bool _translation_SendUntranslatedIfFailed;
    #endregion

    #region Translation - Api
    public const string DESC_Translation_Api_Preset = "API Preset for API Translation";
    public string Translation_Api_Preset
    {
        get => _translation_Api_Preset;
        set => SetProperty(ref _translation_Api_Preset, value);
    }
    private string _translation_Api_Preset = string.Empty;
    #endregion

    #region Voice - General
    public const string DESC_Voice_CurrentSpeakerName = "Name of speaker for voice audio";
    public string Voice_CurrentSpeakerName
    {
        get => _voice_CurrentSpeakerName;
        set => SetProperty(ref _voice_CurrentSpeakerName, value);
    }
    private string _voice_CurrentSpeakerName = string.Empty;

    public const string DESC_Voice_SelectedModuleName = "Name of voice module";
    public string Voice_SelectedModuleName
    {
        get => _voice_SelectedModuleName;
        set => SetProperty(ref _voice_SelectedModuleName, value);
    }
    private string _voice_SelectedModuleName = string.Empty;

    public const string DESC_Voice_AutoStart = "Automatically start voice module on launch";
    public bool Voice_AutoStart
    {
        get => _voice_AutoStart;
        set => SetProperty(ref _voice_AutoStart, value);
    }
    private bool _voice_AutoStart;

    public const string DESC_Voice_AudioVolumePercent = "Volume of voice audio";
    public float Voice_AudioVolumePercent
    {
        get => _voice_AudioVolumePercent;
        set => SetProperty(ref _voice_AudioVolumePercent, value.MinMax(0,1));
    }
    private float _voice_AudioVolumePercent = 0.5f;

    public const string DESC_Voice_MaximumTextLength = "Maximum length of text to be converted to voice";
    public int Voice_MaximumTextLength
    {
        get => _voice_MaximumTextLength;
        set => SetProperty(ref _voice_MaximumTextLength, value.MinMax(1, short.MaxValue));
    }
    private int _voice_MaximumTextLength = 500;

    public const string DESC_Voice_SkipLongerText = "Skips longer text for voice instead of trimming it";
    public bool Voice_SkipLongerText
    {
        get => _voice_SkipLongerText;
        set => SetProperty(ref _voice_SkipLongerText, value);
    }
    private bool _voice_SkipLongerText = true;
    #endregion 

    #region Voice - Azure
    public const string DESC_Voice_Azure_VoiceList = "List of voices to use with Azure TTS";
    public List<AzureTtsVoiceModel> Voice_Azure_VoiceList
    {
        get => _voice_Azure_Voices;
        set => SetProperty(ref _voice_Azure_Voices, value);
    }
    private List<AzureTtsVoiceModel> _voice_Azure_Voices = [];

    public const string DESC_Voice_Azure_CurrentVoice = "Currently selected voice from list";
    public string Voice_Azure_CurrentVoice
    {
        get => _voice_Azure_CurrentVoice;
        set => SetProperty(ref _voice_Azure_CurrentVoice, value);
    }
    private string _voice_Azure_CurrentVoice = string.Empty;

    public const string DESC_Voice_Azure_CustomEndpoint = "Custom endpoint for Azure voices";
    public string Voice_Azure_CustomEndpoint
    {
        get => _voice_Azure_CustomEndpoint;
        set => SetProperty(ref _voice_Azure_CustomEndpoint, value);
    }
    private string _voice_Azure_CustomEndpoint = string.Empty;
    #endregion

    #region Voice - Microsoft
    public const string DESC_Voice_Microsoft_ModelName = "ID of Microsoft TTS model";
    public string Voice_Microsoft_ModelName
    {
        get => _voice_Microsoft_ModelName;
        set => SetProperty(ref _voice_Microsoft_ModelName, value);
    }
    private string _voice_Microsoft_ModelName = string.Empty;
    #endregion

    #region Voice - Piper
    public const string DESC_Voice_Piper_Process_Enabled = "Should a Piper process be started on module start";
    public bool Voice_Piper_Process_Enabled
    {
        get => _voice_Piper_Process_Enabled;
        set => SetProperty(ref _voice_Piper_Process_Enabled, value);
    }
    private bool _voice_Piper_Process_Enabled = false;

    public const string DESC_Voice_Piper_Process_Terminal = "Terminal application to launch Piper process in";
    public string Voice_Piper_Process_Terminal
    {
        get => _voice_Piper_Process_Terminal;
        set => SetProperty(ref _voice_Piper_Process_Terminal, value);
    }
    private string _voice_Piper_Process_Terminal = string.Empty;

    public const string DESC_Voice_Piper_Process_VenvDir = "Path of Python VEnv";
    public string Voice_Piper_Process_VenvDir
    {
        get => _voice_Piper_Process_VenvDir;
        set => SetProperty(ref _voice_Piper_Process_VenvDir, value);
    }
    private string _voice_Piper_Process_VenvDir = string.Empty;

    public const string DESC_Voice_Piper_Process_Voice = "Voice to be set for process";
    public string Voice_Piper_Process_Voice
    {
        get => _voice_Piper_Process_Voice;
        set => SetProperty(ref _voice_Piper_Process_Voice, value);
    }
    private string _voice_Piper_Process_Voice = string.Empty;

    public const string DESC_Voice_Piper_Ip = "Piper webservice IP";
    public string Voice_Piper_Ip
    {
        get => _voice_Piper_Ip;
        set => SetProperty(ref _voice_Piper_Ip, string.IsNullOrWhiteSpace(value) ? "127.0.0.1" : value);
    }
    private string _voice_Piper_Ip = "127.0.0.1";

    public const string DESC_Voice_Piper_Port = "Piper webservice port";
    public ushort Voice_Piper_Port
    {
        get => _voice_Piper_Port;
        set => SetProperty(ref _voice_Piper_Port, value);
    }
    private ushort _voice_Piper_Port = 9101;

    public const string DESC_Voice_Piper_Request_Voice = "Requested voice";
    public string Voice_Piper_Request_Voice
    {
        get => _voice_Piper_Request_Voice;
        set => SetProperty(ref _voice_Piper_Request_Voice, value);
    }
    private string _voice_Piper_Request_Voice = string.Empty;

    public const string DESC_Voice_Piper_Request_NoiseScale = "Piper noise scale";
    public float Voice_Piper_Request_NoiseScale
    {
        get => _voice_Piper_Request_NoiseScale;
        set => SetProperty(ref _voice_Piper_Request_NoiseScale, value.MinMax(-1, 1));
    }
    private float _voice_Piper_Request_NoiseScale = -1;

    public const string DESC_Voice_Piper_Request_NoiseWScale = "Piper noise w scale";
    public float Voice_Piper_Request_NoiseWScale
    {
        get => _voice_Piper_Request_NoiseWScale;
        set => SetProperty(ref _voice_Piper_Request_NoiseWScale, value.MinMax(-1, 1));
    }
    private float _voice_Piper_Request_NoiseWScale = -1;
    #endregion

    #region WebRtc
    public const string DESC_WebRtc_Enabled = "Enables the usage of WebRTC for all compatible microphone inputs";
    public bool WebRtc_Enabled
    {
        get => _webRtc_Enabled;
        set => SetProperty(ref _webRtc_Enabled, value);
    }
    private bool _webRtc_Enabled = true;

    public const string DESC_WebRtc_UseEchoCancellation = "Use WebRTC's echo cancellation";
    public bool WebRtc_UseEchoCancellation
    {
        get => _webRtc_UseEchoCancellation;
        set => SetProperty(ref _webRtc_UseEchoCancellation, value);
    }
    private bool _webRtc_UseEchoCancellation = true;

    public const string DESC_WebRtc_EchoCancellationDelayMs = "Delay (in ms) to use for echo cancellation";
    public int WebRtc_EchoCancellationDelayMs
    {
        get => _webRtc_EchoCancellationDelayMs;
        set => SetProperty(ref _webRtc_EchoCancellationDelayMs, value);
    }
    private int _webRtc_EchoCancellationDelayMs = 40;

    public const string DESC_WebRtc_UseNoiseSuppression = "Use WebRTC's noise suppression";
    public bool WebRtc_UseNoiseSuppression
    {
        get => _webRtc_UseNoiseSuppression;
        set => SetProperty(ref _webRtc_UseNoiseSuppression, value);
    }
    private bool _webRtc_UseNoiseSuppression = true;

    public const string DESC_WebRtc_NoiseSuppressionLevel = "Level of noise suppression strength to use";
    public NoiseSuppressionLevel WebRtc_NoiseSuppressionLevel
    {
        get => _webRtc_NoiseSuppressionLevel;
        set => SetProperty(ref _webRtc_NoiseSuppressionLevel, value);
    }
    private NoiseSuppressionLevel _webRtc_NoiseSuppressionLevel = NoiseSuppressionLevel.Moderate;

    public const string DESC_WebRtc_UseAutomaticGainControl = "Should WebRTC's automatic gain control be used";
    public bool WebRtc_UseAutomaticGainControl
    {
        get => _webRtc_UseAutomaticGainControl;
        set => SetProperty(ref _webRtc_UseAutomaticGainControl, value);
    }
    private bool _webRtc_UseAutomaticGainControl = false;

    public const string DESC_WebRtc_UseHighPassFilter = "Should WebRTC's high pass filter be used";
    public bool WebRtc_UseHighPassFilter
    {
        get => _webRtc_UseHighPassFilter;
        set => SetProperty(ref _webRtc_UseHighPassFilter, value);
    }
    private bool _webRtc_UseHighPassFilter = false;

    public const string DESC_WebRtc_UsePreAmplifier = "Should WebRTC's preamplifier be used";
    public bool WebRtc_UsePreAmplifier
    {
        get => _webRtc_UsePreAmplifier;
        set => SetProperty(ref _webRtc_UsePreAmplifier, value);
    }
    private bool _webRtc_UsePreAmplifier = false;

    public const string DESC_WebRtc_PreAmplifierGainFactor = "Automatic gain factor to use for preamplifier";
    public float WebRtc_PreAmplifierGainFactor
    {
        get => _webRtc_PreAmplifierGainFactor;
        set => SetProperty(ref _webRtc_PreAmplifierGainFactor, value);
    }
    private float _webRtc_PreAmplifierGainFactor = 1;
    #endregion
}