using System.Text;
using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;
using Newtonsoft.Json;
using Serilog;

namespace HoscyCore.Configuration.Legacy;

public static class LegacyConfigModelLoader
{
    public const string DEFAULT_FILE_NAME = "config.json";

    /// <summary>
    /// Loads a legacy config file
    /// </summary>
    /// <returns>Null when no config could be loaded</returns>
    public static LegacyConfigModel? TryLoad(string configFolder, string configFilename, ILogger logger) //Yes I am aware this code is duplicated and should be fixed
    {
        var path = Path.Combine(configFolder, configFilename);
        logger.Information("Attempting to load LegacyConfig at path \"{legacyConfigPath}\"", path);
        try
        {
            if (!Directory.Exists(configFolder)) return null;
            if (!File.Exists(path)) return null;
            string configData = File.ReadAllText(path, Encoding.UTF8);
            var newData = JsonConvert.DeserializeObject<LegacyConfigModel>(configData);
            if (newData is not null)
                return newData;
        }
        catch (JsonReaderException ex)
        {
            logger.Error(ex, "Unable to read legacy JSON file at \"{legacyConfigPath}\" correctly", path);
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error while reading legacy JSON file at \"{legacyConfigPath}\"", path);
            throw;
        }

        return null;
    }

    /// <summary>
    /// Upgrades a LegacyConfigModel
    /// </summary>
    public static LegacyConfigModel Upgrade(this LegacyConfigModel config, ILogger logger)
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
            logger.Debug("Legacy config is already at version {newestVersion}, skipping upgrade", newestVersion);
            return config;
        }
        logger.Information("Legacy config is at version {currentVersion}, newst is {newestVersion}, starting upgrade", config.ConfigVersion, newestVersion);

        foreach (var (version, action) in steps.OrderBy(x => x.Key))
        {
            logger.Debug("Upgrading legacy config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
            try
            {
                action();
                config.ConfigVersion = version;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to upgrade legacy config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
                throw;
            }
            logger.Debug("Upgraded legacy config from version {oldVersion} to version {newVersion}, newest is {newestVersion}", config.ConfigVersion, version, newestVersion);
        }
        logger.Debug("Finished upgrading legacy config to version {newestVersion}", newestVersion);
        return config;
    }

    /// <summary>
    /// Migrates a LegacyConfigModel to a new ConfigModel
    /// </summary>
    public static ConfigModel Migrate(this LegacyConfigModel oldConfig, ILogger logger)
    {
        logger.Information("Migrating legacy config to new format");
        return new ConfigModel()
        {
            ConfigVersion = oldConfig.ConfigVersion,

            Api_Presets = ConvertApiPresetModel(oldConfig.Api.Presets),

            Recognition_Api_Preset = oldConfig.Api.RecognitionPreset,
            Recognition_Api_MaxRecordingTime = oldConfig.Api.RecognitionMaxRecordingTime,

            Translation_Api_Preset = oldConfig.Api.TranslationPreset,
            Translation_SkipLongerMessages = oldConfig.Api.TranslationSkipLongerMessages,
            Translation_MaxTextLength = oldConfig.Api.TranslationMaxTextLength,
            Translation_OfAudioOutput = oldConfig.Api.TranslateTts,
            VrcTextbox_Output_ShowTranslation = oldConfig.Api.TranslateTextbox,
            ExternalInput_DoTranslate = oldConfig.Api.TranslationAllowExternal,
            VrcTextbox_Output_AddOriginalToTranslation = oldConfig.Api.AddOriginalAfterTranslate,

            AzureServices_Region = oldConfig.Api.AzureRegion,
            AzureServices_ApiKey = oldConfig.Api.AzureKey,
            Voice_Azure_CustomEndpoint = oldConfig.Api.AzureCustomEndpointSpeech,
            Recognition_Azure_CustomEndpoint = oldConfig.Api.AzureCustomEndpointRecognition,
            Voice_Azure_CurrentVoice = oldConfig.Api.AzureTtsVoiceCurrent,
            Voice_Azure_VoiceList = ConvertAzureTtsVoiceModel(oldConfig.Api.AzureTtsVoices),
            Recognition_Azure_PresetPhrases = new(oldConfig.Api.AzurePhrases),
            Recognition_Azure_Languages = new(oldConfig.Api.AzureRecognitionLanguages),
            Voice_Azure_OverrideNormal = oldConfig.Api.UseAzureTts,

            ManualInput_SendViaAudio = oldConfig.Input.UseTts,
            ManualInput_SendViaText = oldConfig.Input.UseTextbox,
            ManualInput_DoPreprocessFull = oldConfig.Input.TriggerCommands,
            ManualInput_DoPreprocessPartial = oldConfig.Input.TriggerReplace,
            ManualInput_DoTranslate = oldConfig.Input.AllowTranslation,
            ManualInput_TextPresets = ConvertDictionary(oldConfig.Input.Presets),

            Debug_LogViaCmdOnWindows = oldConfig.Debug.OpenLogWindow,
            Debug_CheckForUpdatesOnStartup = oldConfig.Debug.CheckUpdates,
            Debug_LogMinimumSeverity = Serilog.Events.LogEventLevel.Debug, //we omit this for now
            Debug_LogFilters = ConvertFilterModel(oldConfig.Debug.LogFilters),

            Osc_Routing_TargetIp = oldConfig.Osc.Ip,
            Osc_Routing_TargetPort = oldConfig.Osc.Port.ConvertToUshort(),
            Osc_Routing_ListenPort = oldConfig.Osc.PortListen,
            Osc_Relay_Filters = ConvertOscRoutingFilterModel(oldConfig.Osc.RoutingFilters),

            Osc_Address_Tool_ToggleMute = oldConfig.Osc.AddressManualMute,
            Osc_Address_Tool_SkipAudio = oldConfig.Osc.AddressManualSkipSpeech,
            Osc_Address_Tool_SkipText = oldConfig.Osc.AddressManualSkipBox,
            Osc_Address_Tool_ToggleReplacements = oldConfig.Osc.AddressEnableReplacements,
            Osc_Address_Tool_ToogleOutputToText = oldConfig.Osc.AddressEnableTextbox,
            Osc_Address_Tool_ToggleOutputToAudio = oldConfig.Osc.AddressEnableTts,
            Osc_Address_Tool_ToggleRecognitionAutoMute = oldConfig.Osc.AddressEnableAutoMute,
            Osc_Address_Tool_NotificationForRecognitionListening = oldConfig.Osc.AddressListeningIndicator,

            Osc_Address_Game_Mute = oldConfig.Osc.AddressGameMute,
            Osc_Address_Game_Afk = oldConfig.Osc.AddressGameAfk,
            Osc_Address_Game_Textbox = oldConfig.Osc.AddressGameTextbox,
            Osc_Address_Input_TextMessage = oldConfig.Osc.AddressAddTextbox,
            Osc_Address_Input_AudioMessage = oldConfig.Osc.AddressAddTts,
            Osc_Address_Input_TextNotification = oldConfig.Osc.AddressAddNotification,

            Osc_Address_Media_Pause = oldConfig.Osc.AddressMediaPause,
            Osc_Address_Media_Unpause = oldConfig.Osc.AddressMediaUnpause,
            Osc_Address_Media_Rewind = oldConfig.Osc.AddressMediaRewind,
            Osc_Address_Media_Skip = oldConfig.Osc.AddressMediaSkip,
            Osc_Address_Media_Info = oldConfig.Osc.AddressMediaInfo,
            Osc_Address_Media_Toggle = oldConfig.Osc.AddressMediaToggle,

            Counters_ShowNotification = oldConfig.Osc.ShowCounterNotifications,
            Counters_DisplayDurationSeconds = oldConfig.Osc.CounterDisplayDuration,
            Counters_DisplayCooldownSeconds = oldConfig.Osc.CounterDisplayCooldown,
            Counters_List = ConvertCounterModel(oldConfig.Osc.Counters),

            Afk_ShowDuration = oldConfig.Osc.ShowAfkDuration,
            Afk_BaseDurationDisplayIntervalSeconds = oldConfig.Osc.AfkDuration,
            Afk_TimesDisplayedBeforeDoublingInterval = int.TryParse(Math.Round(oldConfig.Osc.AfkDoubleDuration).ToString(), out var parsed) ? parsed : 0,
            Afk_StartText = oldConfig.Osc.AfkStartText,
            Afk_StopText = oldConfig.Osc.AfkEndText,
            Afk_StatusText = oldConfig.Osc.AfkStatusText,

            Recognition_Send_ViaText = oldConfig.Speech.UseTextbox,
            Recognition_Send_ViaAudio = oldConfig.Speech.UseTts,

            Recognition_Mute_StartUnmuted = oldConfig.Speech.StartUnmuted,
            Recognition_Mute_PlaySound = oldConfig.Speech.PlayMuteSound,
            Recognition_Mute_OnGameMute = oldConfig.Speech.MuteOnVrcMute,

            Audio_CurrentMicrophoneName = oldConfig.Speech.MicId,
            Audio_CurrentSpeakerOutputName = oldConfig.Speech.SpeakerId,

            Recognition_SelectedModuleName = oldConfig.Speech.ModelName,

            Voice_Microsoft_ModelId = oldConfig.Speech.TtsId,
            Voice_AudioVolumePercent = oldConfig.Speech.SpeakerVolumeInt,
            Voice_MaximumTextLength = oldConfig.Speech.MaxLenTtsString,
            Voice_SkipLongerText = oldConfig.Speech.SkipLongerMessages,

            Recognition_Vosk_Models = ConvertDictionary(oldConfig.Speech.VoskModels),
            Recognition_Vosk_CurrentModel = oldConfig.Speech.VoskModelCurrent,
            Recognition_Vosk_NewWordWaitTimeMs = oldConfig.Speech.VoskTimeout,

            Recognition_Whisper_Models = ConvertDictionary(oldConfig.Speech.WhisperModels),
            Recognition_Whisper_SelectedModel = oldConfig.Speech.WhisperModelCurrent,
            Recognition_Whisper_Cfg_UseSingleSegmentMode = oldConfig.Speech.WhisperSingleSegment,
            Recognition_Whisper_Cfg_TranslateToEnglish = oldConfig.Speech.WhisperToEnglish,
            Recognition_Whisper_Fix_RemoveRandomBrackets = oldConfig.Speech.WhisperBracketFix,
            Recognition_Whisper_Dbg_LogFilteredNoises = oldConfig.Speech.WhisperLogFilteredNoises,
            
            Recognition_Whisper_Cfg_NoiseFilter = ConvertDictionary(oldConfig.Speech.WhisperNoiseFilter),
            Recognition_Whisper_CfgAdv_ThreadsUsed = oldConfig.Speech.WhisperThreads,
            Recognition_Whisper_CfgAdv_MaxSegmentLength = oldConfig.Speech.WhisperMaxSegLen,

            Recognition_Windows_ModelId = oldConfig.Speech.WinModelId,

            Recognition_Fixup_NoiseFilter = new(oldConfig.Speech.NoiseFilter),
            Recognition_Fixup_RemoveEndPeriod = oldConfig.Speech.RemovePeriod,
            Recognition_Fixup_CapitalizeFirstLetter = oldConfig.Speech.CapitalizeFirst,
            Preprocessing_DoReplacementsPartial = oldConfig.Speech.UseReplacements,
            Preprocessing_ReplacementsFull = ConvertReplacementDataModel(oldConfig.Speech.Shortcuts),
            Preprocessing_ReplacementsPartial = ConvertReplacementDataModel(oldConfig.Speech.Replacements),
            Preprocessing_ReplacementFullIgnoredCharacters = oldConfig.Speech.ShortcutIgnoredCharacters,

            VrcTextbox_Output_MaxDisplayedCharacters = oldConfig.Textbox.MaxLength,
            VrcTextbox_Do_Output = oldConfig.Speech.UseTextbox || oldConfig.Input.UseTextbox,
            VrcTextbox_Do_Indicator = oldConfig.Textbox.UseIndicatorWithoutBox || oldConfig.Textbox.UseIndicatorWhenSpeaking,

            VrcTextbox_Timeout_DynamicPer20CharactersDisplayedMs = oldConfig.Textbox.TimeoutMultiplier,
            VrcTextbox_Timeout_DynamicMinimumMs = oldConfig.Textbox.MinimumTimeout,
            VrcTextbox_Timeout_StaticMs = oldConfig.Textbox.DefaultTimeout,
            VrcTextbox_Timeout_UseDynamic = oldConfig.Textbox.DynamicTimeout,
            VrcTextbox_Timeout_AutomaticallyClearNotification = oldConfig.Textbox.AutomaticClearNotification,
            VrcTextbox_Timeout_AutomaticallyClearMessage = oldConfig.Textbox.AutomaticClearMessage,

            VrcTextbox_Notification_IndicatorTextStart = oldConfig.Textbox.NotificationIndicatorLeft,
            VrcTextbox_Notification_IndicatorTextEnd = oldConfig.Textbox.NotificationIndicatorRight,
            VrcTextbox_Notification_UsePrioritySystem = oldConfig.Textbox.UseNotificationPriority,
            VrcTextbox_Notification_SkipWhenMessageAvailable = oldConfig.Textbox.UseNotificationSkip,

            VrcTextbox_Sound_OnMessage = oldConfig.Textbox.SoundOnMessage,
            VrcTextbox_Sound_OnNotification = oldConfig.Textbox.SoundOnNotification,

            Media_ShowStatus = oldConfig.Textbox.MediaShowStatus,
            Media_AddAlbumToText = oldConfig.Textbox.MediaAddAlbum,
            Media_SwapArtistAndSongInText = oldConfig.Textbox.MediaSwapArtistAndSong,
            Media_PlayingVerb = oldConfig.Textbox.MediaPlayingVerb,
            Media_IntermediateWord = oldConfig.Textbox.MediaArtistVerb,
            Media_AlbumWord = oldConfig.Textbox.MediaAlbumVerb,
            Media_ExtraText = oldConfig.Textbox.MediaExtra,
            Media_Filters = ConvertFilterModel(oldConfig.Textbox.MediaFilters)
        };
    }

    #region Conversion Helpers
    private static List<ReplacementDataModel> ConvertReplacementDataModel(List<LegacyReplacementDataModel> replacements)
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

    private static List<CounterModel> ConvertCounterModel(List<LegacyCounterModel> counters)
    {
        return new(counters.Select(old => new CounterModel()
        {
            Name = old.Name,
            Count = old.Count,
            LastUsed = old.LastUsed,
            Enabled = old.Enabled,
            CooldownSeconds = old.Cooldown,
            Parameter = old.Parameter
        }));
    }

    private static List<OscRelayFilterModel> ConvertOscRoutingFilterModel(List<LegacyOscRoutingFilterModel> routingFilters)
    {
        return new(routingFilters.Select(old => new OscRelayFilterModel()
        {
            Name = old.Name,
            Port = old.Port.ConvertToUshort(),
            Ip = old.Ip,
            Filters = new(old.Filters),
            BlacklistMode = old.BlacklistMode
        }));
    }

    private static List<FilterModel> ConvertFilterModel(List<LegacyFilterModel> filters)
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

    private static Dictionary<Tkey, Tvalue> ConvertDictionary<Tkey, Tvalue>(Dictionary<Tkey, Tvalue> dict) where Tkey : notnull
    {
        return new(dict.Select(old => new KeyValuePair<Tkey, Tvalue>(old.Key, old.Value)));
    }

    private static List<AzureTtsVoiceModel> ConvertAzureTtsVoiceModel(List<LegacyAzureTtsVoiceModel> azureTtsVoices)
    {
        return new(azureTtsVoices.Select(old => new AzureTtsVoiceModel()
        {
            Name = old.Name,
            Voice = old.Voice,
            Language = old.Language
        }));
    }

    private static List<ApiPresetModel> ConvertApiPresetModel(List<LegacyApiPresetModel> presets)
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