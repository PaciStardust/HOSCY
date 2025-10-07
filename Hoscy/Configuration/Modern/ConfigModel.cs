using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoscy.Utility;
using Serilog.Core;
using Serilog.Events;
using Whisper;

namespace Hoscy.Configuration.Modern;

public class ConfigModel : ObservableObject
{
    public int ConfigVersion { get; set; } = 0;

    #region ApiCommunication

    private ObservableCollection<ApiPresetModel> _apiCommunication_Presets = [];
    public ObservableCollection<ApiPresetModel> ApiCommunication_Presets
    {
        get => _apiCommunication_Presets;
        set => SetProperty(ref _apiCommunication_Presets, value);
    }
    public int ApiCommunication_Presets_GetIndex(string name)
        => ApiCommunication_Presets.GetListIndex(x => x.Name == name);

    //RECOGNITION
    private string _apiCommunication_Recognition_CurrentPreset = string.Empty;
    public string ApiCommunication_Recognition_CurrentPreset
    {
        get => _apiCommunication_Recognition_CurrentPreset;
        set => SetProperty(ref _apiCommunication_Recognition_CurrentPreset, value);
    }

    private int _apiCommunication_Recognition_MaxRecordingTime = 30;
    public int ApiCommunication_Recognition_MaxRecordingTime
    {
        get => _apiCommunication_Recognition_MaxRecordingTime;
        set => SetProperty(ref _apiCommunication_Recognition_MaxRecordingTime, Utils.MinMax(value, 1, 300));
    }

    //TRANSLATION
    private string _apiCommunication_Translation_CurrentPreset = string.Empty;
    public string ApiCommunication_Translation_CurrentPreset
    {
        get => _apiCommunication_Translation_CurrentPreset;
        set => SetProperty(ref _apiCommunication_Translation_CurrentPreset, value);
    }

    private bool _apiCommunication_Translation_SkipLongerMessages = true;
    public bool ApiCommunication_Translation_SkipLongerMessages
    {
        get => _apiCommunication_Translation_SkipLongerMessages;
        set => SetProperty(ref _apiCommunication_Translation_SkipLongerMessages, value);
    }

    private int _apiCommunication_Translation_MaxTextLength = 2000;
    public int ApiCommunication_Translation_MaxTextLength
    {
        get => _apiCommunication_Translation_MaxTextLength;
        set => SetProperty(ref _apiCommunication_Translation_MaxTextLength, Utils.MinMax(value, 1, short.MaxValue));
    }

    private bool _apiCommunication_Translation_OfTts;
    public bool ApiCommunication_Translation_OfTts
    {
        get => _apiCommunication_Translation_OfTts;
        set => SetProperty(ref _apiCommunication_Translation_OfTts, value);
    }

    private bool _apiCommunication_Translation_OfTextbox;
    public bool ApiCommunication_Translation_OfTextbox
    {
        get => _apiCommunication_Translation_OfTextbox;
        set => SetProperty(ref _apiCommunication_Translation_OfTextbox, value);
    }

    private bool _apiCommunication_Translation_OfExternalSources;
    public bool ApiCommunication_Translation_OfExternalSources
    {
        get => _apiCommunication_Translation_OfExternalSources;
        set => SetProperty(ref _apiCommunication_Translation_OfExternalSources, value);
    }

    private bool _apiCommunication_Translation_AppendOriginal;
    public bool ApiCommunication_Translation_AppendOriginal
    {
        get => _apiCommunication_Translation_AppendOriginal;
        set => SetProperty(ref _apiCommunication_Translation_AppendOriginal, value);
    }

    //AZURE
    private string _apiCommunication_Azure_Region = string.Empty;
    public string ApiCommunication_Azure_Region
    {
        get => _apiCommunication_Azure_Region;
        set => SetProperty(ref _apiCommunication_Azure_Region, value);
    }

    private string _apiCommunication_Azure_Key = string.Empty;
    public string ApiCommunication_Azure_Key
    {
        get => _apiCommunication_Azure_Key;
        set => SetProperty(ref _apiCommunication_Azure_Key, value);
    }

    private string _apiCommunication_Azure_CustomEndpointSpeech = string.Empty;
    public string ApiCommunication_Azure_CustomEndpointSpeech
    {
        get => _apiCommunication_Azure_CustomEndpointSpeech;
        set => SetProperty(ref _apiCommunication_Azure_CustomEndpointSpeech, value);
    }

    private string _apiCommunication_Azure_CustomEndpointRecognition = string.Empty;
    public string ApiCommunication_Azure_CustomEndpointRecognition
    {
        get => _apiCommunication_Azure_CustomEndpointRecognition;
        set => SetProperty(ref _apiCommunication_Azure_CustomEndpointRecognition, value);
    }

    private string _apiCommunication_Azure_CurrentTtsVoice = string.Empty;
    public string ApiCommunication_Azure_CurrentTtsVoice
    {
        get => _apiCommunication_Azure_CurrentTtsVoice;
        set => SetProperty(ref _apiCommunication_Azure_CurrentTtsVoice, value);
    }

    private ObservableCollection<AzureTtsVoiceModel> _apiCommunication_Azure_TtsVoices = [];
    public ObservableCollection<AzureTtsVoiceModel> ApiCommunication_Azure_TtsVoices
    {
        get => _apiCommunication_Azure_TtsVoices;
        set => SetProperty(ref _apiCommunication_Azure_TtsVoices, value);
    }
    public int GetTtsVoiceIndex(string name)
            => ApiCommunication_Azure_TtsVoices.GetListIndex(x => x.Name == name);

    private ObservableCollection<string> _apiCommunication_Azure_Phrases = [];
    public ObservableCollection<string> ApiCommunication_Azure_Phrases
    {
        get => _apiCommunication_Azure_Phrases;
        set => SetProperty(ref _apiCommunication_Azure_Phrases, value);
    }

    private ObservableCollection<string> _apiCommunication_Azure_RecognitionLanguages = [];
    public ObservableCollection<string> ApiCommunication_Azure_RecognitionLanguages
    {
        get => _apiCommunication_Azure_RecognitionLanguages;
        set => SetProperty(ref _apiCommunication_Azure_RecognitionLanguages, value);
    }

    private bool _apiCommunication_Azure_OverrideNormalTts;
    public bool ApiCommunication_Azure_OverrideNormalTts
    {
        get => _apiCommunication_Azure_OverrideNormalTts;
        set => SetProperty(ref _apiCommunication_Azure_OverrideNormalTts, value);
    }

    #endregion

    #region Input

    private bool _input_UseTts;
    public bool Input_UseTts
    {
        get => _input_UseTts;
        set => SetProperty(ref _input_UseTts, value);
    }

    private bool _input_UseTextbox = true;
    public bool Input_UseTextbox
    {
        get => _input_UseTextbox;
        set => SetProperty(ref _input_UseTextbox, value);
    }

    private bool _input_CanTriggerCommands = true;
    public bool Input_CanTriggerCommands
    {
        get => _input_CanTriggerCommands;
        set => SetProperty(ref _input_CanTriggerCommands, value);
    }

    private bool _input_CanTriggerReplace = true;
    public bool Input_CanTriggerReplace
    {
        get => _input_CanTriggerReplace;
        set => SetProperty(ref _input_CanTriggerReplace, value);
    }

    private bool _input_CanBeTranslated = true;
    public bool Input_CanBeTranslated
    {
        get => _input_CanBeTranslated;
        set => SetProperty(ref _input_CanBeTranslated, value);
    }

    private ObservableCollection<KeyValuePair<string, string>> _input_Presets = [];
    public ObservableCollection<KeyValuePair<string, string>> Input_Presets
    {
        get => _input_Presets;
        set => SetProperty(ref _input_Presets, value);
    }

    #endregion

    #region Logger 

    private bool _logger_OpenWindowOnStartupWindowsOnly;
    public bool Logger_OpenWindowOnStartupWindowsOnly
    {
        get => _logger_OpenWindowOnStartupWindowsOnly;
        set => SetProperty(ref _logger_OpenWindowOnStartupWindowsOnly, value);
    }

    private bool _logger_LogToCommandline = true;
    public bool Logger_LogToCommandLine
    {
        get => _logger_LogToCommandline;
        set => SetProperty(ref _logger_LogToCommandline, value);
    }

    private bool _logger_CheckForUpdatesOnStartup = true;
    public bool Logger_CheckForUpdatesOnStartup
    {
        get => _logger_CheckForUpdatesOnStartup;
        set => SetProperty(ref _logger_CheckForUpdatesOnStartup, value);
    }

    private LogEventLevel _logger_MinimumSeverity = LogEventLevel.Debug;
    private readonly LoggingLevelSwitch _logger_MinimumSeveritySwitch = new(LogEventLevel.Debug);
    public LogEventLevel Logger_MinimumSeverity
    {
        get => _logger_MinimumSeverity;
        set
        {
            SetProperty(ref _logger_MinimumSeverity, value);
            _logger_MinimumSeveritySwitch.MinimumLevel = value;
        }
    }
    public LoggingLevelSwitch Logger_MinimumSeverityGetSwitch()
        => _logger_MinimumSeveritySwitch;

    private ObservableCollection<FilterModel> _logger_Filters = [];
    public ObservableCollection<FilterModel> Logger_Filters
    {
        get => _logger_Filters;
        set => SetProperty(ref _logger_Filters, value);
    }

    #endregion

    #region OSC

    //ROUTING
    private string _osc_Routing_TargetIp = "127.0.0.1";
    public string Osc_Routing_TargetIp
    {
        get => _osc_Routing_TargetIp;
        set => SetProperty(ref _osc_Routing_TargetIp, value);
    }

    private ushort _osc_Routing_TargetPort = 9000;
    public ushort Osc_Routing_TargetPort
    {
        get => _osc_Routing_TargetPort;
        set => SetProperty(ref _osc_Routing_TargetPort, Utils.MinMax(value, ushort.MinValue, ushort.MaxValue));
    }

    private int _osc_Routing_ListenPort = 9001;
    public int Osc_Routing_ListenPort
    {
        get => _osc_Routing_ListenPort;
        set => SetProperty(ref _osc_Routing_ListenPort, Utils.MinMax(value, -1, 65535));
    }

    private ObservableCollection<OscRelayFilterModel> _osc_Relay_Filters = [];
    public ObservableCollection<OscRelayFilterModel> Osc_Relay_Filters
    {
        get => _osc_Relay_Filters;
        set => SetProperty(ref _osc_Relay_Filters, value);
    }

    private bool _osc_Relay_IgnoreIfHandled = true;
    public bool Osc_Relay_IgnoreIfHandled
    {
        get => _osc_Relay_IgnoreIfHandled;
        set => SetProperty(ref _osc_Relay_IgnoreIfHandled, value);
    }

    //Address - Tool
    private string _osc_Address_Tool_ManualMute = "/avatar/parameters/ToolMute";
    public string Osc_Address_Tool_ManualMute
    {
        get => _osc_Address_Tool_ManualMute;
        set => SetProperty(ref _osc_Address_Tool_ManualMute, value);
    }

    private string _osc_Address_Tool_SkipSpeech = "/avatar/parameters/ToolSkipSpeech";
    public string Osc_Address_Tool_SkipSpeech
    {
        get => _osc_Address_Tool_SkipSpeech;
        set => SetProperty(ref _osc_Address_Tool_SkipSpeech, value);
    }

    private string _osc_Address_Tool_SkipTextbox = "/avatar/parameters/ToolSkipBox";
    public string Osc_Address_Tool_SkipTextbox
    {
        get => _osc_Address_Tool_SkipTextbox;
        set => SetProperty(ref _osc_Address_Tool_SkipTextbox, value);
    }

    private string _osc_Address_Tool_EnableReplacements = "/avatar/parameters/ToolEnableReplacements";
    public string Osc_Address_Tool_EnableReplacements
    {
        get => _osc_Address_Tool_EnableReplacements;
        set => SetProperty(ref _osc_Address_Tool_EnableReplacements, value);
    }

    private string _osc_Address_Tool_EnableTextbox = "/avatar/parameters/ToolEnableBox";
    public string Osc_Address_Tool_EnableTextbox
    {
        get => _osc_Address_Tool_EnableTextbox;
        set => SetProperty(ref _osc_Address_Tool_EnableTextbox, value);
    }

    private string _osc_Address_Tool_EnableTts = "/avatar/parameters/ToolEnableTts";
    public string Osc_Address_Tool_EnableTts
    {
        get => _osc_Address_Tool_EnableTts;
        set => SetProperty(ref _osc_Address_Tool_EnableTts, value);
    }

    private string _osc_Address_Tool_EnableAutoMute = "/avatar/parameters/ToolEnableAutoMute";
    public string Osc_Address_Tool_EnableAutoMute
    {
        get => _osc_Address_Tool_EnableAutoMute;
        set => SetProperty(ref _osc_Address_Tool_EnableAutoMute, value);
    }

    private string _osc_Address_Tool_SetMicListening = "/avatar/parameters/MicListening";
    public string Osc_Address_Tool_SetMicListening
    {
        get => _osc_Address_Tool_SetMicListening;
        set => SetProperty(ref _osc_Address_Tool_SetMicListening, value);
    }

    //Address - Game
    private string _osc_Address_Game_Mute = "/avatar/parameters/MuteSelf";
    public string Osc_Address_Game_Mute
    {
        get => _osc_Address_Game_Mute;
        set => SetProperty(ref _osc_Address_Game_Mute, value);
    }

    private string _osc_Address_Game_Afk = "/avatar/parameters/AFK";
    public string Osc_Address_Game_Afk
    {
        get => _osc_Address_Game_Afk;
        set => SetProperty(ref _osc_Address_Game_Afk, value);
    }

    private string _osc_Address_Game_Textbox = "/chatbox/input";
    public string Osc_Address_Game_Textbox
    {
        get => _osc_Address_Game_Textbox;
        set => SetProperty(ref _osc_Address_Game_Textbox, value);
    }

    //Address - Input
    private string _osc_Address_Input_TextboxMessage = "/hoscy/message";
    public string Osc_Address_Input_TextboxMessage
    {
        get => _osc_Address_Input_TextboxMessage;
        set => SetProperty(ref _osc_Address_Input_TextboxMessage, value);
    }

    private string _osc_Address_Input_Tts = "/hoscy/tts";
    public string Osc_Address_Input_Tts
    {
        get => _osc_Address_Input_Tts;
        set => SetProperty(ref _osc_Address_Input_Tts, value);
    }

    private string _osc_Address_Input_TextboxNotification = "/hoscy/notification";
    public string Osc_Address_Input_TextboxNotification
    {
        get => _osc_Address_Input_TextboxNotification;
        set => SetProperty(ref _osc_Address_Input_TextboxNotification, value);
    }

    //Address - Media
    private string _osc_Address_Media_Pause = "/avatar/parameters/MediaPause";
    public string Osc_Address_Media_Pause
    {
        get => _osc_Address_Media_Pause;
        set => SetProperty(ref _osc_Address_Media_Pause, value);
    }

    private string _osc_Address_Media_Unpause = "/avatar/parameters/MediaUnpause";
    public string Osc_Address_Media_Unpause
    {
        get => _osc_Address_Media_Unpause;
        set => SetProperty(ref _osc_Address_Media_Unpause, value);
    }

    private string _osc_Address_Media_Rewind = "/avatar/parameters/MediaRewind";
    public string Osc_Address_Media_Rewind
    {
        get => _osc_Address_Media_Rewind;
        set => SetProperty(ref _osc_Address_Media_Rewind, value);
    }

    private string _osc_Address_Media_Skip = "/avatar/parameters/MediaSkip";
    public string Osc_Address_Media_Skip
    {
        get => _osc_Address_Media_Skip;
        set => SetProperty(ref _osc_Address_Media_Skip, value);
    }

    private string _osc_Address_Media_Info = "/avatar/parameters/MediaInfo";
    public string Osc_Address_Media_Info
    {
        get => _osc_Address_Media_Info;
        set => SetProperty(ref _osc_Address_Media_Info, value);
    }

    private string _osc_Address_Media_Toggle = "/avatar/parameters/MediaToggle";
    public string Osc_Address_Media_Toggle
    {
        get => _osc_Address_Media_Toggle;
        set => SetProperty(ref _osc_Address_Media_Toggle, value);
    }

    //COUNTERS
    private bool _osc_Counters_ShowNotification;
    public bool Osc_Counters_ShowNotification
    {
        get => _osc_Counters_ShowNotification;
        set => SetProperty(ref _osc_Counters_ShowNotification, value);
    }

    private float _osc_Counters_DisplayDuration = 10f;
    public float Osc_Counters_DisplayDuration
    {
        get => _osc_Counters_DisplayDuration;
        set => SetProperty(ref _osc_Counters_DisplayDuration, Utils.MinMax(value, 0.01f, 30));
    }

    private float _osc_Counters_DisplayCooldown = 0f;
    public float Osc_Counters_DisplayCooldown
    {
        get => _osc_Counters_DisplayCooldown;
        set => SetProperty(ref _osc_Counters_DisplayCooldown, Utils.MinMax(value, 0, 300));
    }

    private ObservableCollection<CounterModel> _osc_Counters_List = [];
    public ObservableCollection<CounterModel> Osc_Counters_List
    {
        get => _osc_Counters_List;
        set => SetProperty(ref _osc_Counters_List, value);
    }

    //AFK
    private bool _osc_Afk_ShowDuration = false;
    public bool Osc_Afk_ShowDuration
    {
        get => _osc_Afk_ShowDuration;
        set => SetProperty(ref _osc_Afk_ShowDuration, value);
    }

    private float _osc_Afk_BaseDurationDisplayIntervalSeconds = 15f;
    public float Osc_Afk_BaseDurationDisplayIntervalSeconds
    {
        get => _osc_Afk_BaseDurationDisplayIntervalSeconds;
        set => SetProperty(ref _osc_Afk_BaseDurationDisplayIntervalSeconds, Utils.MinMax(value, 5, 300));
    }

    private int _osc_Afk_TimesDisplayedBeforeDoublingInterval = 12;
    public int Osc_Afk_TimesDisplayedBeforeDoublingInterval
    {
        get => _osc_Afk_TimesDisplayedBeforeDoublingInterval;
        set => SetProperty(ref _osc_Afk_TimesDisplayedBeforeDoublingInterval, Utils.MinMax(value, 0, 60));
    }

    private const string OSC_AFK_NO_STARTTEXT = "Now AFK";
    private string _osc_Afk_StartText = OSC_AFK_NO_STARTTEXT;
    public string OSC_Afk_StartText
    {
        get => _osc_Afk_StartText;
        set => SetProperty(ref _osc_Afk_StartText, value.Length > 0 ? value : OSC_AFK_NO_STARTTEXT);
    }

    private const string OSC_AFK_NO_ENDTEXT = "No longer AFK";
    private string _osc_Afk_EndText = OSC_AFK_NO_ENDTEXT;
    public string Osc_Afk_EndText
    {
        get => _osc_Afk_EndText;
        set => SetProperty(ref _osc_Afk_EndText, value.Length > 0 ? value : OSC_AFK_NO_ENDTEXT);
    }

    private const string OSC_AFK_NO_STATUSTEXT = "AFK since";
    private string _osc_Afk_StatusText = OSC_AFK_NO_STATUSTEXT;
    public string Osc_Afk_StatusText
    {
        get => _osc_Afk_StatusText;
        set => SetProperty(ref _osc_Afk_StatusText, value.Length > 0 ? value : OSC_AFK_NO_STATUSTEXT);
    }

    #endregion

    #region Speech

    // SEND
    private bool _speech_Send_OverTextbox = true;
    public bool Speech_Send_OverTextbox
    {
        get => _speech_Send_OverTextbox;
        set => SetProperty(ref _speech_Send_OverTextbox, value);
    }

    private bool _speech_Send_OverTts = false;
    public bool Speech_Send_OverTts
    {
        get => _speech_Send_OverTts;
        set => SetProperty(ref _speech_Send_OverTts, value);
    }

    // MUTECONTROL
    private bool _speech_Mute_StartUnmuted = true;
    public bool Speech_Mute_StartUnmuted
    {
        get => _speech_Mute_StartUnmuted;
        set => SetProperty(ref _speech_Mute_StartUnmuted, value);
    }

    private bool _speech_Mute_PlaySound = false;
    public bool Speech_Mute_PlaySound
    {
        get => _speech_Mute_PlaySound;
        set => SetProperty(ref _speech_Mute_PlaySound, value);
    }

    private bool _speech_Mute_OnGameMute = true;
    public bool Speech_Mute_OnGameMute
    {
        get => _speech_Mute_OnGameMute;
        set => SetProperty(ref _speech_Mute_OnGameMute, value);
    }

    // DEVICES
    private string _speech_Device_CurrentMicrophoneId = string.Empty;
    public string Speech_Device_CurrentMicrophoneId
    {
        get => _speech_Device_CurrentMicrophoneId;
        set => SetProperty(ref _speech_Device_CurrentMicrophoneId, value);
    }

    private string _speech_Device_CurrentSpeakerId = string.Empty;
    public string Speech_Device_CurrentSpeakerId
    {
        get => _speech_Device_CurrentSpeakerId;
        set => SetProperty(ref _speech_Device_CurrentSpeakerId, value);
    }

    // SHARED
    private string _speech_Shared_ModelName = string.Empty;
    public string Speech_Shared_ModelName
    {
        get => _speech_Shared_ModelName;
        set => SetProperty(ref _speech_Shared_ModelName, value);
    }

    // TTS
    private string _speech_Tts_MicrosoftTtsId = string.Empty;
    public string Speech_Tts_MicrosoftTtsId
    {
        get => _speech_Tts_MicrosoftTtsId;
        set => SetProperty(ref _speech_Tts_MicrosoftTtsId, value);
    }

    private int _speech_Tts_AudioVolumePercent = 50;
    public int Speech_Tts_AudioVolumePercent
    {
        get => _speech_Tts_AudioVolumePercent;
        set => SetProperty(ref _speech_Tts_AudioVolumePercent, value);
    }

    private int _speech_Tts_MaximumLength = 500;
    public int Speech_Tts_MaximumLength
    {
        get => _speech_Tts_MaximumLength;
        set => SetProperty(ref _speech_Tts_MaximumLength, Utils.MinMax(value, 1, short.MaxValue));
    }

    private bool _speech_Tts_SkipLongerMessages = true;
    public bool Speech_Tts_SkipLongerMessages
    {
        get => _speech_Tts_SkipLongerMessages;
        set => SetProperty(ref _speech_Tts_SkipLongerMessages, value);
    }

    // VOSK
    private ObservableCollection<KeyValuePair<string, string>> _speech_Vosk_Models = [];
    public ObservableCollection<KeyValuePair<string, string>> Speech_Vosk_Models
    {
        get => _speech_Vosk_Models;
        set => SetProperty(ref _speech_Vosk_Models, value);
    }

    private string _speech_Vosk_CurrentModel = string.Empty;
    public string Speech_Vosk_CurrentModel
    {
        get => _speech_Vosk_CurrentModel;
        set => SetProperty(ref _speech_Vosk_CurrentModel, value);
    }

    private int _speech_Vosk_NewWordWaitTimeMs = 2500;
    public int Speech_Vosk_NewWordWaitTimeMs
    {
        get => _speech_Vosk_NewWordWaitTimeMs;
        set => SetProperty(ref _speech_Vosk_NewWordWaitTimeMs, Utils.MinMax(value, 500, 30000));
    }

    // WHISPER
    private ObservableCollection<KeyValuePair<string, string>> _speech_Whisper_Models = [];
    public ObservableCollection<KeyValuePair<string, string>> Speech_Whisper_Models
    {
        get => _speech_Whisper_Models;
        set => SetProperty(ref _speech_Whisper_Models, value);
    }

    private string _speech_Whisper_CurrentModel = string.Empty;
    public string Speech_Whisper_CurrentModel
    {
        get => _speech_Whisper_CurrentModel;
        set => SetProperty(ref _speech_Whisper_CurrentModel, value);
    }

    private bool _speech_Whisper_UseSingleSegmentMode = true; // (Higher accuracy, reduced functionality)
    public bool Speech_Whisper_UseSingleSegmentMode
    {
        get => _speech_Whisper_UseSingleSegmentMode;
        set => SetProperty(ref _speech_Whisper_UseSingleSegmentMode, value);
    }

    private bool _speech_Whisper_TranslateToEnglish = false;
    public bool Speech_Whisper_TranslateToEnglish
    {
        get => _speech_Whisper_TranslateToEnglish;
        set => SetProperty(ref _speech_Whisper_TranslateToEnglish, value);
    }

    private bool _speech_Whisper_UseBracketFix = true; //Fixes the bracket issue ('( ( (')
    public bool Speech_Whisper_UseBracketFix
    {
        get => _speech_Whisper_UseBracketFix;
        set => SetProperty(ref _speech_Whisper_UseBracketFix, value);
    }

    private bool _speech_Whisper_IncreaseThreadPriority = false;
    public bool Speech_Whisper_IncreaseThreadPriority
    {
        get => _speech_Whisper_IncreaseThreadPriority;
        set => SetProperty(ref _speech_Whisper_IncreaseThreadPriority, value);
    }

    private bool _speech_Whisper_LogFilteredNoises = false;
    public bool Speech_Whisper_LogFilteredNoises
    {
        get => _speech_Whisper_LogFilteredNoises;
        set => SetProperty(ref _speech_Whisper_LogFilteredNoises, value);
    }

    private eLanguage _speech_Whisper_Language = eLanguage.English;
    public eLanguage Speech_Whisper_Language
    {
        get => _speech_Whisper_Language;
        set => SetProperty(ref _speech_Whisper_Language, value);
    }

    private ObservableCollection<KeyValuePair<string, string>> _speech_Whisper_NoiseFilter = [];
    public ObservableCollection<KeyValuePair<string, string>> Speech_Whisper_NoiseFilter
    {
        get => _speech_Whisper_NoiseFilter;
        set => SetProperty(ref _speech_Whisper_NoiseFilter, value);
    }

    private int _speech_Whisper_ThreadsUsed = -4; // 0 = All, -n = All but n
    public int Speech_Whisper_ThreadUsed
    {
        get => _speech_Whisper_ThreadsUsed;
        set => SetProperty(ref _speech_Whisper_ThreadsUsed, Utils.MinMax(value, short.MinValue, short.MaxValue));
    }

    private int _speech_Whisper_MaxContext = 0;
    public int Speech_Whisper_MaxContext
    {
        get => _speech_Whisper_MaxContext;
        set => SetProperty(ref _speech_Whisper_MaxContext, Utils.MinMax(value, -1, short.MaxValue));
    }

    private int _speech_Whisper_MaxSegmentLength = 0;
    public int Speech_Whisper_MaxSegmentLength
    {
        get => _speech_Whisper_MaxSegmentLength;
        set => SetProperty(ref _speech_Whisper_MaxSegmentLength, Utils.MinMax(value, 0, short.MaxValue));
    }

    private float _speech_Whisper_MaxRecognitionDurationSeconds = 16;
    public float Speech_Whisper_MaxRecognitionDurationSeconds
    {
        get => _speech_Whisper_MaxRecognitionDurationSeconds;
        set => SetProperty(ref _speech_Whisper_MaxRecognitionDurationSeconds, Utils.MinMax(value, 2, short.MaxValue));
    }

    private float _speech_Whisper_RecognitionPauseDurationSeconds = 0.5f;
    public float Speech_Whisper_RecognitionPauseDurationSeconds
    {
        get => _speech_Whisper_RecognitionPauseDurationSeconds;
        set => SetProperty(ref _speech_Whisper_RecognitionPauseDurationSeconds, Utils.MinMax(value, 0.05f, short.MaxValue));
    }

    private string _speech_Whisper_GraphicsAdapter = string.Empty;
    public string Speech_Whisper_GraphicsAdapter
    {
        get => _speech_Whisper_GraphicsAdapter;
        set => SetProperty(ref _speech_Whisper_GraphicsAdapter, value);
    }

    // Windows
    private string _speech_Windows_ModelId = string.Empty;
    public string Speech_Windows_ModelId
    {
        get => _speech_Windows_ModelId;
        set => SetProperty(ref _speech_Windows_ModelId, value);
    }

    // Replacement
    private ObservableCollection<string> _speech_Replacement_NoiseFilter = [];
    public ObservableCollection<string> Speech_Replacement_NoiseFilter
    {
        get => _speech_Replacement_NoiseFilter;
        set => SetProperty(ref _speech_Replacement_NoiseFilter, value);
    }

    private bool _speech_Replacement_RemoveEndPeriod = true;
    public bool Speech_Replacement_RemoveEndPeriod
    {
        get => _speech_Replacement_RemoveEndPeriod;
        set => SetProperty(ref _speech_Replacement_RemoveEndPeriod, value);
    }

    private bool _speech_Replacement_CapitalizeFirstLetter = true;
    public bool Speech_Replacement_CapitalizeFirstLetter
    {
        get => _speech_Replacement_CapitalizeFirstLetter;
        set => SetProperty(ref _speech_Replacement_CapitalizeFirstLetter, value);
    }

    private bool _speech_Replacement_IsEnabled = true;
    public bool Speech_Replacement_IsEnabled
    {
        get => _speech_Replacement_IsEnabled;
        set => SetProperty(ref _speech_Replacement_IsEnabled, value);
    }

    private ObservableCollection<ReplacementDataModel> _speech_Replacement_Shortcuts = [];
    public ObservableCollection<ReplacementDataModel> Speech_Replacement_Shortcuts
    {
        get => _speech_Replacement_Shortcuts;
        set => SetProperty(ref _speech_Replacement_Shortcuts, value);
    }

    private ObservableCollection<ReplacementDataModel> _speech_Replacement_Replacements = [];
    public ObservableCollection<ReplacementDataModel> Speech_Replacement_Replacements
    {
        get => _speech_Replacement_Replacements;
        set => SetProperty(ref _speech_Replacement_Replacements, value);
    }

    private string _speech_Replacement_IgnoredCharactersForShortcuts = ".?!,。、！？";
    public string Speech_Replacement_IgnoredCharactersForShortcuts
    {
        get => _speech_Replacement_IgnoredCharactersForShortcuts;
        set => SetProperty(ref _speech_Replacement_IgnoredCharactersForShortcuts, value);
    }

    #endregion

    #region Textbox

    // Text
    private int _textbox_Text_MaxDisplayedCharacters = 130;
    public int Textbox_Text_MaxDisplayedCharacters
    {
        get => _textbox_Text_MaxDisplayedCharacters;
        set => SetProperty(ref _textbox_Text_MaxDisplayedCharacters, Utils.MinMax(value, 10, 130));
    }

    private bool _textbox_Text_TypingIndicatorWhenSpeaking;
    public bool Textbox_Text_TypingIndicatorWhenSpeaking
    {
        get => _textbox_Text_TypingIndicatorWhenSpeaking;
        set => SetProperty(ref _textbox_Text_TypingIndicatorWhenSpeaking, value);
    }

    private bool _textbox_Text_TypingIndicatorWhenDisabled;
    public bool Textbox_Text_TypingIndicatorWhenDisabled
    {
        get => _textbox_Text_TypingIndicatorWhenDisabled;
        set => SetProperty(ref _textbox_Text_TypingIndicatorWhenDisabled, value);
    }

    // Timeout
    private int _textbox_Timeout_DynamicPer20CharactersDisplayedMs = 1250;
    public int Textbox_Timeout_DynamicPer20CharactersDisplayedMs
    {
        get => _textbox_Timeout_DynamicPer20CharactersDisplayedMs;
        set => SetProperty(ref _textbox_Timeout_DynamicPer20CharactersDisplayedMs, Utils.MinMax(value, 250, 10000));
    }

    private int _textbox_Timeout_DynamicMinimumMs = 3000;
    public int Textbox_Timeout_DynamicMinimumMs
    {
        get => _textbox_Timeout_DynamicMinimumMs;
        set => SetProperty(ref _textbox_Timeout_DynamicMinimumMs, Utils.MinMax(value, 1250, 30000));
    }

    private int _textbox_Timeout_StaticMs = 5000;
    public int Textbox_Timeout_StaticMs
    {
        get => _textbox_Timeout_StaticMs;
        set => SetProperty(ref _textbox_Timeout_StaticMs, Utils.MinMax(value, 1250, 30000));
    }

    private bool _textbox_Timeout_UseDynamic = true;
    public bool Textbox_Timeout_UseDynamic
    {
        get => _textbox_Timeout_UseDynamic;
        set => SetProperty(ref _textbox_Timeout_UseDynamic, value);
    }

    private bool _textbox_Timeout_AutomaticallyClearNotification = true;
    public bool Textbox_Timeout_AutomaticallyClearNotification
    {
        get => _textbox_Timeout_AutomaticallyClearNotification;
        set => SetProperty(ref _textbox_Timeout_AutomaticallyClearNotification, value);
    }

    private bool _textbox_Timeout_AutomaticallyClearMessage;
    public bool Textbox_Timeout_AutomaticallyClearMessage
    {
        get => _textbox_Timeout_AutomaticallyClearMessage;
        set => SetProperty(ref _textbox_Timeout_AutomaticallyClearMessage, value);
    }

    // Notification
    private string _textbox_Notification_IndicatorTextStart = "〈";
    private string _textbox_Notification_IndicatorTextEnd = "〉";
    private int _textbox_Notification_IndicatorTextLength = 2;
    public string Textbox_Notification_IndicatorTextStart //Text to the left of a notification
    {
        get => _textbox_Notification_IndicatorTextStart;
        set
        {
            SetProperty(ref _textbox_Notification_IndicatorTextStart, value.Length < 4 ? value : value[..3]);
            _textbox_Notification_IndicatorTextLength = CalcNotificationIndicatorLength();
        }
    }
    public string Textbox_Notification_IndicatorTextEnd //Text to the right of a notification
    {
        get => _textbox_Notification_IndicatorTextEnd;
        set
        {
            SetProperty(ref _textbox_Notification_IndicatorTextEnd, value.Length < 4 ? value : value[..3]);
            _textbox_Notification_IndicatorTextLength = CalcNotificationIndicatorLength();
        }
    }
    private int CalcNotificationIndicatorLength()
    => _textbox_Notification_IndicatorTextEnd.Length + _textbox_Notification_IndicatorTextStart.Length;
    internal int NotificationIndicatorLength() => _textbox_Notification_IndicatorTextLength;

    private bool _textbox_Notification_UsePrioritySystem = true;
    public bool Textbox_Notification_UsePrioritySystem
    {
        get => _textbox_Notification_UsePrioritySystem;
        set => SetProperty(ref _textbox_Notification_UsePrioritySystem, value);
    }

    private bool _textbox_Notification_SkipWhenMessageAvailable = true;
    public bool Textbox_Notification_SkipWhenMessageAvailable
    {
        get => _textbox_Notification_SkipWhenMessageAvailable;
        set => SetProperty(ref _textbox_Notification_SkipWhenMessageAvailable, value);
    }

    // Sound
    private bool _textbox_Sound_OnMessage = true;
    public bool Textbox_Sound_OnMessage
    {
        get => _textbox_Sound_OnMessage;
        set => SetProperty(ref _textbox_Sound_OnMessage, value);
    }

    private bool _textbox_Sound_OnNotification;
    public bool Textbox_Sound_OnNotification
    {
        get => _textbox_Sound_OnNotification;
        set => SetProperty(ref _textbox_Sound_OnNotification, value);
    }

    // Media
    private bool _textbox_Media_ShowStatus;
    public bool Textbox_Media_ShowStatus
    {
        get => _textbox_Media_ShowStatus;
        set => SetProperty(ref _textbox_Media_ShowStatus, value);
    }

    private bool _textbox_Media_AddAlbumToText;
    public bool Textbox_Media_AddAlbumToText
    {
        get => _textbox_Media_AddAlbumToText;
        set => SetProperty(ref _textbox_Media_AddAlbumToText, value);
    }

    private bool _textbox_Media_SwapArtistAndSongInText;
    public bool Textbox_Media_SwapArtistAndSongInText
    {
        get => _textbox_Media_SwapArtistAndSongInText;
        set => SetProperty(ref _textbox_Media_SwapArtistAndSongInText, value);
    }

    private const string NO_MEDIA_PLAYINGVERB = "Playing";
    private string _textbox_Media_PlayingVerb = NO_MEDIA_PLAYINGVERB;
    public string Textbox_Media_PlayingVerb
    {
        get => _textbox_Media_PlayingVerb;
        set => SetProperty(ref _textbox_Media_PlayingVerb, value.Length > 0 ? value : NO_MEDIA_PLAYINGVERB);
    }

    private const string NO_MEDIA_ARTISTVERB = "by";
    private string _textbox_Media_ArtistVerb = NO_MEDIA_ARTISTVERB;
    public string Textbox_Media_ArtistVerb
    {
        get => _textbox_Media_ArtistVerb;
        set => SetProperty(ref _textbox_Media_ArtistVerb, value.Length > 0 ? value : NO_MEDIA_ARTISTVERB);
    }

    private const string NO_MEDIA_ALBUMVERB = "on";
    private string _textbox_Media_AlbumVerb = NO_MEDIA_ALBUMVERB;
    public string Textbox_Media_AlbumVerb
    {
        get => _textbox_Media_AlbumVerb;
        set => SetProperty(ref _textbox_Media_AlbumVerb, value.Length > 0 ? value : NO_MEDIA_ALBUMVERB);
    }

    private string _textbox_Media_ExtraText = string.Empty;
    public string Textbox_Media_ExtraText
    {
        get => _textbox_Media_ExtraText;
        set => SetProperty(ref _textbox_Media_ExtraText, value);
    }

    private ObservableCollection<FilterModel> _textbox_Media_Filters = [];
    public ObservableCollection<FilterModel> Textbox_Media_Filters
    {
        get => _textbox_Media_Filters;
        set => SetProperty(ref _textbox_Media_Filters, value);
    }

    #endregion
}