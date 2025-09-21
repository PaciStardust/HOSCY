using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace Hoscy.Models.Config.Migration;

internal static class LegacyConfigModelLoader
{
    internal static LegacyConfigModel? Load(string path, ILogger logger)
    {
        logger.Information("Attempting to load LegacyConfig at path {legacyConfigPath}", path);
        try
        {
            if (!File.Exists(path)) return null;
            string configData = File.ReadAllText(Utils.PathConfigFile, Encoding.UTF8);
            var newData = JsonConvert.DeserializeObject<LegacyConfigModel>(configData);
            if (newData is not null)
                return newData;
        }
        catch (JsonReaderException ex)
        {
            logger.Error(ex, "Unable to read legacy JSON file at {legacyConfigPath} correctly", path);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error while reading legacy JSON file at {legacyConfigPath}", path);
        }

        return null;
    }

    internal static LegacyConfigModel? Upgrade(this LegacyConfigModel config, ILogger logger)
    {
        Dictionary<int, Action> steps = new()
        {
            { 1, () => {
                if (config.Speech.NoiseFilter.Count == 0)
                {
                    config.Speech.NoiseFilter =
                    [
                        "the",
                        "and",
                        "einen"
                    ];
                }
                if (config.Speech.Replacements.Count == 0) {
                    config.Speech.Replacements =
                    [
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
                    ];
                }
            }},
            {2, () => {
                config.Speech.Shortcuts.Add(new("box toggle", "[osc] [/avatar/parameters/ToolEnableBox [b]true \"self\"]"));
                if (config.Api.Presets.Count != 0) return;
                config.Api.Presets =
                [
                    new()
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
                    new()
                    {
                        Name = "Example - Azure Recognition",
                        TargetUrl = "https://northeurope.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US",
                        ResultField = "Display Text",
                        SentData = string.Empty,
                        ContentType = "audio/wav; codecs=audio/pcm; samplerate=16000",
                        HeaderValues =
                        {
                            { "Ocp-Apim-Subscription-Key", "[YOUR KEY]" },
                            { "Accept", "true" }
                        }
                    },
                    new()
                    {
                        Name = "Example - DeepL to DE",
                        TargetUrl = "https://api-free.deepl.com/v2/translate",
                        ResultField = "text",
                        SentData = "text=[T]&target_lang=DE",
                        ContentType = "application/x-www-form-urlencoded",
                        Authorization = "DeepL-Auth-Key [YOUR KEY]"
                    }
                ];
            }},
            {4, () => {
                if (config.Debug.LogFilters.Count != 0) return;
                config.Debug.LogFilters = [
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
                ];
            }},
            {5, () => {
                if (config.Speech.WhisperNoiseFilter.Count != 0) return;
                config.Speech.WhisperNoiseFilter = new()
                {
                    { "Laughing", "laugh" },
                    { "Popping", "pop" },
                    { "Whistling", "whistl" },
                    { "Sighing", "sigh" },
                    { "Humming", "hum" }
                };
            }}
        };

        var newestVersion = steps.Keys.Max();
        if (config.ConfigVersion == newestVersion)
        {
            logger.Information("Legacy config is already at version {newestVersion}, skipping upgrade", newestVersion);
        }
        logger.Information("Legacy config is at version {currentVersion}, newst is {newestVersion}, starting upgrade", config.ConfigVersion, newestVersion);

        foreach (var (version, action) in steps.OrderBy(x => x.Key))
        {
            logger.Information("Upgrading legacy config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
            try
            {
                action();
                config.ConfigVersion = version;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to upgrade legacy config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
                return null;
            }
            logger.Information("Upgraded legacy config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
        }
        logger.Information("Finished upgrading legacy config to version {newestVersion}", newestVersion);
        return config;
    }

    internal static ConfigModel Migrate(this LegacyConfigModel oldConfig, ILogger logger)
    {
        logger.Information("Migrating legacy config to new format");
        return new ConfigModel()
        {
            ConfigVersion = oldConfig.ConfigVersion,

            ApiCommunication_Presets = ConvertApiPresetModel(oldConfig.Api.Presets),

            ApiCommunication_Recognition_CurrentPreset = oldConfig.Api.RecognitionPreset,
            ApiCommunication_Recognition_MaxRecordingTime = oldConfig.Api.RecognitionMaxRecordingTime,

            ApiCommunication_Translation_CurrentPreset = oldConfig.Api.TranslationPreset,
            ApiCommunication_Translation_SkipLongerMessages = oldConfig.Api.TranslationSkipLongerMessages,
            ApiCommunication_Translation_MaxTextLength = oldConfig.Api.TranslationMaxTextLength,
            ApiCommunication_Translation_OfTts = oldConfig.Api.TranslateTts,
            ApiCommunication_Translation_OfTextbox = oldConfig.Api.TranslateTextbox,
            ApiCommunication_Translation_OfExternalSources = oldConfig.Api.TranslationAllowExternal,
            ApiCommunication_Translation_AppendOriginal = oldConfig.Api.AddOriginalAfterTranslate,

            ApiCommunication_Azure_Region = oldConfig.Api.AzureRegion,
            ApiCommunication_Azure_Key = oldConfig.Api.AzureKey,
            ApiCommunication_Azure_CustomEndpointSpeech = oldConfig.Api.AzureCustomEndpointSpeech,
            ApiCommunication_Azure_CustomEndpointRecognition = oldConfig.Api.AzureCustomEndpointRecognition,
            ApiCommunication_Azure_CurrentTtsVoice = oldConfig.Api.AzureTtsVoiceCurrent,
            ApiCommunication_Azure_TtsVoices = ConvertAzureTtsVoiceModel(oldConfig.Api.AzureTtsVoices),
            ApiCommunication_Azure_Phrases = new(oldConfig.Api.AzurePhrases),
            ApiCommunication_Azure_RecognitionLanguages = new(oldConfig.Api.AzureRecognitionLanguages),
            ApiCommunication_Azure_OverrideNormalTts = oldConfig.Api.UseAzureTts,

            Input_UseTts = oldConfig.Input.UseTts,
            Input_UseTextbox = oldConfig.Input.UseTextbox,
            Input_CanTriggerCommands = oldConfig.Input.TriggerCommands,
            Input_CanTriggerReplace = oldConfig.Input.TriggerReplace,
            Input_CanBeTranslated = oldConfig.Input.AllowTranslation,
            Input_Presets = ConvertDictionary(oldConfig.Input.Presets),

            Logger_OpenWindowOnStartup = oldConfig.Debug.OpenLogWindow,
            Logger_CheckForUpdatesOnStartup = oldConfig.Debug.CheckUpdates,
            Logger_MinimumSeverity = "Log", //we omit this for now
            Logger_Filters = ConvertFilterModel(oldConfig.Debug.LogFilters),

            Osc_Routing_TargetIp = oldConfig.Osc.Ip,
            Osc_Routing_TargetPort = oldConfig.Osc.Port,
            Osc_Routing_ListenPort = oldConfig.Osc.PortListen,
            Osc_Routing_Filters = ConvertOscRoutingFilterModel(oldConfig.Osc.RoutingFilters),

            Osc_Address_Tool_ManualMute = oldConfig.Osc.AddressManualMute,
            Osc_Address_Tool_SkipSpeech = oldConfig.Osc.AddressManualSkipSpeech,
            Osc_Address_Tool_SkipTextbox = oldConfig.Osc.AddressManualSkipBox,
            Osc_Address_Tool_EnableReplacements = oldConfig.Osc.AddressEnableReplacements,
            Osc_Address_Tool_EnableTextbox = oldConfig.Osc.AddressEnableTextbox,
            Osc_Address_Tool_EnableTts = oldConfig.Osc.AddressEnableTts,
            Osc_Address_Tool_EnableAutoMute = oldConfig.Osc.AddressEnableAutoMute,
            Osc_Address_Tool_SetMicListening = oldConfig.Osc.AddressListeningIndicator,

            Osc_Address_Game_Mute = oldConfig.Osc.AddressGameMute,
            Osc_Address_Game_Afk = oldConfig.Osc.AddressGameAfk,
            Osc_Address_Game_Textbox = oldConfig.Osc.AddressGameTextbox,
            Osc_Address_Input_TextboxMessage = oldConfig.Osc.AddressAddTextbox,
            Osc_Address_Input_Tts = oldConfig.Osc.AddressAddTts,
            Osc_Address_Input_TextboxNotification = oldConfig.Osc.AddressAddNotification,

            Osc_Address_Media_Pause = oldConfig.Osc.AddressMediaPause,
            Osc_Address_Media_Unpause = oldConfig.Osc.AddressMediaUnpause,
            Osc_Address_Media_Rewind = oldConfig.Osc.AddressMediaRewind,
            Osc_Address_Media_Skip = oldConfig.Osc.AddressMediaSkip,
            Osc_Address_Media_Info = oldConfig.Osc.AddressMediaInfo,
            Osc_Address_Media_Toggle = oldConfig.Osc.AddressMediaToggle,

            Osc_Counters_ShowNotification = oldConfig.Osc.ShowCounterNotifications,
            Osc_Counters_DisplayDuration = oldConfig.Osc.CounterDisplayDuration,
            Osc_Counters_DisplayCooldown = oldConfig.Osc.CounterDisplayCooldown,
            Osc_Counters_List = ConvertCounterModel(oldConfig.Osc.Counters),

            Osc_Afk_ShowDuration = oldConfig.Osc.ShowAfkDuration,
            Osc_Afk_BaseDurationDisplayIntervalSeconds = oldConfig.Osc.AfkDuration,
            Osc_Afk_TimesDisplayedBeforeDoublingInterval = int.TryParse(Math.Round(oldConfig.Osc.AfkDoubleDuration).ToString(), out var parsed) ? parsed : 0,
            OSC_Afk_StartText = oldConfig.Osc.AfkStartText,
            Osc_Afk_EndText = oldConfig.Osc.AfkEndText,
            Osc_Afk_StatusText = oldConfig.Osc.AfkStatusText,

            Speech_Send_OverTextbox = oldConfig.Speech.UseTextbox,
            Speech_Send_OverTts = oldConfig.Speech.UseTts,

            Speech_Mute_StartUnmuted = oldConfig.Speech.StartUnmuted,
            Speech_Mute_PlaySound = oldConfig.Speech.PlayMuteSound,
            Speech_Mute_OnGameMute = oldConfig.Speech.MuteOnVrcMute,

            Speech_Device_CurrentMicrophoneId = oldConfig.Speech.MicId,
            Speech_Device_CurrentSpeakerId = oldConfig.Speech.SpeakerId,

            Speech_Shared_ModelName = oldConfig.Speech.ModelName,

            Speech_Tts_MicrosoftTtsId = oldConfig.Speech.TtsId,
            Speech_Tts_AudioVolumePercent = oldConfig.Speech.SpeakerVolumeInt,
            Speech_Tts_MaximumLength = oldConfig.Speech.MaxLenTtsString,
            Speech_Tts_SkipLongerMessages = oldConfig.Speech.SkipLongerMessages,

            Speech_Vosk_Models = ConvertDictionary(oldConfig.Speech.VoskModels),
            Speech_Vosk_CurrentModel = oldConfig.Speech.VoskModelCurrent,
            Speech_Vosk_NewWordWaitTimeMs = oldConfig.Speech.VoskTimeout,

            Speech_Whisper_Models = ConvertDictionary(oldConfig.Speech.WhisperModels),
            Speech_Whisper_CurrentModel = oldConfig.Speech.WhisperModelCurrent,
            Speech_Whisper_UseSingleSegmentMode = oldConfig.Speech.WhisperSingleSegment,
            Speech_Whisper_TranslateToEnglish = oldConfig.Speech.WhisperToEnglish,
            Speech_Whisper_UseBracketFix = oldConfig.Speech.WhisperBracketFix,
            Speech_Whisper_IncreaseThreadPriority = oldConfig.Speech.WhisperHighPerformance,
            Speech_Whisper_LogFilteredNoises = oldConfig.Speech.WhisperLogFilteredNoises,
            Speech_Whisper_Language = oldConfig.Speech.WhisperLanguage,
            Speech_Whisper_NoiseFilter = ConvertDictionary(oldConfig.Speech.WhisperNoiseFilter),
            Speech_Whisper_ThreadUsed = oldConfig.Speech.WhisperThreads,
            Speech_Whisper_MaxContext = oldConfig.Speech.WhisperMaxContext,
            Speech_Whisper_MaxSegmentLength = oldConfig.Speech.WhisperMaxSegLen,
            Speech_Whisper_MaxRecognitionDurationSeconds = oldConfig.Speech.WhisperRecMaxDuration,
            Speech_Whisper_RecognitionPauseDurationSeconds = oldConfig.Speech.WhisperRecPauseDuration,
            Speech_Whisper_GraphicsAdapter = oldConfig.Speech.WhisperGraphicsAdapter,

            Speech_Windows_ModelId = oldConfig.Speech.WinModelId,

            Speech_Replacement_NoiseFilter = new(oldConfig.Speech.NoiseFilter),
            Speech_Replacement_RemoveEndPeriod = oldConfig.Speech.RemovePeriod,
            Speech_Replacement_CapitalizeFirstLetter = oldConfig.Speech.CapitalizeFirst,
            Speech_Replacement_IsEnabled = oldConfig.Speech.UseReplacements,
            Speech_Replacement_Shortcuts = ConvertReplacementDataModel(oldConfig.Speech.Shortcuts),
            Speech_Replacement_Replacements = ConvertReplacementDataModel(oldConfig.Speech.Replacements),
            Speech_Replacement_IgnoredCharactersForShortcuts = oldConfig.Speech.ShortcutIgnoredCharacters,

            Textbox_Text_MaxDisplayedCharacters = oldConfig.Textbox.MaxLength,
            Textbox_Text_TypingIndicatorWhenDisabled = oldConfig.Textbox.UseIndicatorWithoutBox,

            Textbox_Timeout_DynamicPer20CharactersDisplayedMs = oldConfig.Textbox.TimeoutMultiplier,
            Textbox_Timeout_DynamicMinimumMs = oldConfig.Textbox.MinimumTimeout,
            Textbox_Timeout_StaticMs = oldConfig.Textbox.DefaultTimeout,
            Textbox_Timeout_UseDynamic = oldConfig.Textbox.DynamicTimeout,
            Textbox_Timeout_AutomaticallyClearNotification = oldConfig.Textbox.AutomaticClearNotification,
            Textbox_Timeout_AutomaticallyClearMessage = oldConfig.Textbox.AutomaticClearMessage,

            Textbox_Notification_IndicatorTextStart = oldConfig.Textbox.NotificationIndicatorLeft,
            Textbox_Notification_IndicatorTextEnd = oldConfig.Textbox.NotificationIndicatorRight,
            Textbox_Notification_UsePrioritySystem = oldConfig.Textbox.UseNotificationPriority,
            Textbox_Notification_SkipWhenMessageAvailable = oldConfig.Textbox.UseNotificationSkip,

            Textbox_Sound_OnMessage = oldConfig.Textbox.SoundOnMessage,
            Textbox_Sound_OnNotification = oldConfig.Textbox.SoundOnNotification,

            Textbox_Media_ShowStatus = oldConfig.Textbox.MediaShowStatus,
            Textbox_Media_AddAlbumToText = oldConfig.Textbox.MediaAddAlbum,
            Textbox_Media_SwapArtistAndSongInText = oldConfig.Textbox.MediaSwapArtistAndSong,
            Textbox_Media_PlayingVerb = oldConfig.Textbox.MediaPlayingVerb,
            Textbox_Media_ArtistVerb = oldConfig.Textbox.MediaArtistVerb,
            Textbox_Media_AlbumVerb = oldConfig.Textbox.MediaAlbumVerb,
            Textbox_Media_ExtraText = oldConfig.Textbox.MediaExtra,
            Textbox_Media_Filters = ConvertFilterModel(oldConfig.Textbox.MediaFilters)
        };
    }

    #region Conversion Helpers
    private static ObservableCollection<ReplacementDataModel> ConvertReplacementDataModel(List<LegacyReplacementDataModel> replacements)
    {
        return new(replacements.Select(old => new ReplacementDataModel()
        {
            Text = old.Text,
            Replacement = old.Replacement,
            Enabled = old.Enabled,
            UseRegex = old.UseRegex,
            IgnoreCase = old.IgnoreCase,
        }));
    }

    private static ObservableCollection<CounterModel> ConvertCounterModel(List<LegacyCounterModel> counters)
    {
        return new(counters.Select(old => new CounterModel()
        {
            Name = old.Name,
            Count = old.Count,
            LastUsed = old.LastUsed,
            Enabled = old.Enabled,
            Cooldown = old.Cooldown,
            Parameter = old.Parameter
        }));
    }

    private static ObservableCollection<OscRoutingFilterModel> ConvertOscRoutingFilterModel(List<LegacyOscRoutingFilterModel> routingFilters)
    {
        return new(routingFilters.Select(old => new OscRoutingFilterModel()
        {
            Name = old.Name,
            Port = old.Port,
            Ip = old.Ip,
            Filters = new(old.Filters),
            BlacklistMode = old.BlacklistMode
        }));
    }

    private static ObservableCollection<FilterModel> ConvertFilterModel(List<LegacyFilterModel> filters)
    {
        return new(filters.Select(old => new FilterModel()
        {
            Name = old.Name,
            FilterString = old.FilterString,
            Enabled = old.Enabled,
            IgnoreCase = old.IgnoreCase,
            UseRegex = old.UseRegex
        }));
    }

    private static ObservableCollection<KeyValuePair<Tkey, Tvalue>> ConvertDictionary<Tkey, Tvalue>(Dictionary<Tkey, Tvalue> dict) where Tkey : notnull
    {
        return new(dict.Select(old => new KeyValuePair<Tkey, Tvalue>(old.Key, old.Value)));
    }

    private static ObservableCollection<AzureTtsVoiceModel> ConvertAzureTtsVoiceModel(List<LegacyAzureTtsVoiceModel> azureTtsVoices)
    {
        return new(azureTtsVoices.Select(old => new AzureTtsVoiceModel()
        {
            Name = old.Name,
            Voice = old.Voice,
            Language = old.Language
        }));
    }

    private static ObservableCollection<ApiPresetModel> ConvertApiPresetModel(List<LegacyApiPresetModel> presets)
    {
        return new(presets.Select(old => new ApiPresetModel()
        {
            Name = old.Name,
            SentData = old.SentData,
            HeaderValues = ConvertDictionary(old.HeaderValues),
            ContentType = old.ContentType,
            ResultField = old.ResultField,
            TargetUrl = old.TargetUrl,
            Authorization = old.Authorization,
            ConnectionTimeout = old.ConnectionTimeout
        }));
    }
    #endregion
}