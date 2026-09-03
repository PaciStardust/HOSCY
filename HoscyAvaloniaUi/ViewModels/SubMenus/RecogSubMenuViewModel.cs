using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Services.Recognition.Extra;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class RecogSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    [ObservableProperty]
    public partial ComboBoxData OptionsSelectedModule { get; set; }
    [ObservableProperty]
    public partial string OptionsSelectedModuleDescription { get; protected set; }
    [ObservableProperty]
    public partial IBrush OptionsSelectedModuleStartStopBrush { get; protected set; }
    [ObservableProperty]
    public partial string OptionsSelectedModuleStartStopText { get; protected set; }
    [ObservableProperty]
    public partial IBrush OptionsSelectedModuleToggleMuteBrush { get; protected set; }
    [ObservableProperty]
    public partial string OptionsSelectedModuleToggleMuteText { get; protected set; }
    [ObservableProperty]
    public partial bool OptionsSelectedModuleToggleMuteEnabled { get; protected set; }
    [ObservableProperty]
    public partial bool OptionsSelectedModuleRestartEnabled { get; protected set; }
    [ObservableProperty]
    public partial bool OptionsSelectedModuleRestartNeeded { get; protected set; }
    public virtual void OptionsSelectedModuleChanged() { }
    public virtual void OptionsSelectedModuleStartStopClicked() { }
    public virtual void OptionsSelectedModuleRefreshClicked() { }
    public virtual void OptionsSelectedModuleRestartClicked() { }
    public virtual void OptionsSelectedModuleToggleMuteClicked() { }

    public virtual void OptionsOutputNoiseFilterClicked() { }

    [ObservableProperty]
    public partial bool OptionsMicrophoneAvailable { get; protected set; }
    [ObservableProperty]
    public partial ComboBoxData OptionsMicrophone { get; set; }
    public virtual void OptionsMicrophoneChanged() { }
    public virtual void OptionsMicrophoneRefreshClicked() { }

    [ObservableProperty]
    public partial string ModulesSettingsVisibleIfCompatible { get; protected set; } = "(Settings are visible if compatible recognition module is selected)";

    [ObservableProperty]
    public partial bool ModulesAnyApiIsSelected { get; protected set; }
    [ObservableProperty]
    public partial ComboBoxData ModulesAnyApiPresets { get; set; }
    public virtual void ModulesAnyApiEditPresets() { }
    public virtual void ModulesAnyApiPresetChanged() { }

    [ObservableProperty]
    public partial bool ModulesAzureIsSelected { get; protected set; }
    public virtual void ModulesAzureEditPresetPhrases() { }
    public virtual void ModulesAzureEditLanguages() { }

    [ObservableProperty]
    public partial bool ModulesVoskIsSelected { get; protected set; }
    [ObservableProperty]
    public partial ComboBoxData ModulesVoskModels { get; set; }
    public virtual void ModulesVoskEditModels() { }
    public virtual void ModulesVoskModelChanged() { }

    [ObservableProperty]
    public partial bool ModulesWhisperIsSelected { get; protected set; }
    [ObservableProperty]
    public partial ComboBoxData ModulesWhisperModels { get; set; }
    [ObservableProperty]
    public partial ComboBoxData ModulesWhisperVadMode { get; set; }
    [ObservableProperty]
    public partial bool ModulesWhisperShowAdvancedSettings { get; set; }
    public virtual void ModulesWhisperEditModels() { }
    public virtual void ModulesWhisperModelChanged() { }
    public virtual void ModulesWhisperEditNoiseFilter() { }
    public virtual void ModulesWhisperVadModeChanged() { }

    [ObservableProperty]
    public partial bool ModulesWindowsIsSelected { get; protected set; }
    [ObservableProperty]
    public partial ComboBoxData ModulesWindowsModels { get; set; }
    [ObservableProperty]
    public partial string ModulesWindowsModelDescription { get; set; }
    public virtual void ModulesWindowsModelChanged() { }
}

[PrototypeLoadIntoDiContainer(typeof(RecogSubMenuViewModelBase), Lifetime.Transient)]
public class RecogSubMenuViewModelImpl : RecogSubMenuViewModelBase //todo: [FEAT] Change indicator?
{
    private readonly ILogger _logger;
    private readonly IAudioService _audio;
    private readonly PopupWindowFactory _popup;
    private readonly IRecognitionManagerService _recognition;
    private readonly IRecognitionModuleStartInfo[] _recognitionInfos;
    private readonly UiHelperService _uiHelper;

    #if WINDOWS
    private readonly Dictionary<string,(string Desc,string Id)> _windowsModels;
    #endif

    public RecogSubMenuViewModelImpl
    (
        ConfigModel config, 
        ILogger logger, 
        IAudioService audio, 
        IBackToFrontNotifyService notify,
        PopupWindowFactory popup,
        IRecognitionManagerService recognition,
        UiHelperService uiHelper
    )
    {
        Config = config;
        _logger = logger.ForContext<RecogSubMenuViewModelImpl>();
        _audio = audio;
        _popup = popup;
        _recognition = recognition;
        _uiHelper = uiHelper;

        OptionsSelectedModuleUpdateButtons(_recognition.GetCurrentModuleStatus(), _recognition.IsListening);
        _recognition.OnModuleStatusChanged += OptionsSelectedModuleOnStatusChanged;

        _recognitionInfos = [.. _recognition.GetModuleInfos().OrderByDescending(x => x.Priority)];
        OptionsSelectedModule = new([.. _recognitionInfos.Select(x => x.Name)], Config.Recognition_SelectedModuleName, _logger, "OptionsSelectedModule");
        OptionsSelectedModuleUpdateComboBox();

        List<ResMsg> errors = [];
        var mics = OptionsMicrophoneGetNames();
        mics.IfFail(errors.Add);
        OptionsMicrophone = new(mics.Value ?? [], Config.Recognition_MicrophoneName, _logger, "OptionsMicrophone");

        ModulesAnyApiPresets = new([.. Config.Api_Presets.Select(x => x.Name)], Config.Recognition_Api_Preset, _logger, "ModulesAnyApiPresets");

        ModulesVoskModels = new([.. Config.Recognition_Vosk_Models.Keys], Config.Recognition_Vosk_CurrentModel, _logger, "ModulesVoskModels");

        ModulesWhisperModels = new([.. Config.Recognition_Whisper_Models.Keys], Config.Recognition_Whisper_SelectedModel, _logger, "ModulesWhisperModels");
        ModulesWhisperVadMode = new([.. Enum.GetNames<WhisperIpcVadOperatingMode>()], Enum.GetName(Config.Recognition_Whisper_Cfg_VadOperatingMode) ?? string.Empty, _logger, "ModulesWhisperVadMode");

        #if WINDOWS
        _windowsModels = [];
        var winModels = WinApi.GetWindowsRecognizers(_logger);
        winModels.IfFail(errors.Add);
        foreach(var model in winModels.Value ?? [])
        {
            _windowsModels[model.Name] = (model.Desc, model.Id);
        }
        ModulesWindowsModels = new([.. _windowsModels.Keys], _windowsModels.FirstOrDefault(x => x.Value.Id == Config.Recognition_Windows_ModelId).Key, _logger, "ModulesWindowsModels");
        ModulesWindowsModelsUpdateComboBox();
        #else
        ModulesWindowsModels = new();
        ModulesWindowsModelDescription = "This feature is not supported outside of Windows";
        #endif

        if (errors.Count > 0)
        {
            var error = ResC.FailM(errors);
            notify.SendResult("Some data could not be loaded", error.Msg!);
        }
    }

    public override void OptionsSelectedModuleChanged()
    {
        OptionsSelectedModuleUpdateComboBox();
    }
    private void OptionsSelectedModuleUpdateComboBox()
    {
        var description =  "Description: ";
        var flags = RecognitionModuleConfigFlags.None;

        var selected = OptionsSelectedModule.GetSelected();
        if (selected is null)
        {
            description += "No module is selected";
        }
        else
        {
            var match = _recognitionInfos.FirstOrDefault(x => x.Name == selected);
            if (match is null)
            {
                description += "Selected module not found";
            }
            else
            {
                description += match.Description;
                flags = match.ConfigFlags;
            }
        }

        Config.Recognition_SelectedModuleName = selected ?? string.Empty;

        OptionsSelectedModuleDescription = description;
        OptionsMicrophoneAvailable = flags.HasFlag(RecognitionModuleConfigFlags.Microphone);

        ModulesAnyApiIsSelected = flags.HasFlag(RecognitionModuleConfigFlags.AnyApi);
        ModulesAzureIsSelected = flags.HasFlag(RecognitionModuleConfigFlags.Azure);
        ModulesVoskIsSelected = flags.HasFlag(RecognitionModuleConfigFlags.Vosk);
        ModulesWhisperIsSelected = flags.HasFlag(RecognitionModuleConfigFlags.Whisper);
        ModulesWindowsIsSelected = flags.HasFlag(RecognitionModuleConfigFlags.Windows);
    }
    private void OptionsSelectedModuleOnStatusChanged(object? sender, RecognitionStatusChangedEventArgs e)
    {
        OptionsSelectedModuleUpdateButtons(e.Status, e.IsListening);
    }
    public override void OptionsSelectedModuleStartStopClicked()
    {
        if (_recognition.GetCurrentModuleStatus() == ServiceStatus.Stopped)
        {
        _logger.Information("Starting recognition module");
            OptionsSelectedModuleStartStopText = "Starting";
            var res = _recognition.StartModule();
            res.IfFail(x => _popup.OpenNotification("Failed to start recognition module", x.Message, true, true));
        } 
        else
        {
            _logger.Information("Stopping recognition module");
            OptionsSelectedModuleStartStopText = "Stopping";
            var res = _recognition.StopModule();
            res.IfFail(x => _popup.OpenNotification("Failed to stop recognition module", x.Message, true, true));
        }
    }
    public override void OptionsSelectedModuleToggleMuteClicked()
    {
        if (_recognition.GetCurrentModuleStatus() != ServiceStatus.Stopped)
        {
            var value = !_recognition.IsListening;
            _logger.Information("Setting recognition listening status to {value}", value);
            var res = _recognition.SetListening(value);
            res.IfFail(x => _popup.OpenNotification("Failed to set recognition module listening status", x.Message, true, true));
        }
    }
    public override void OptionsSelectedModuleRefreshClicked()
    {
        _logger.Information("Refreshing recognition module");
        var res = _recognition.RefreshModule();
        res.IfFail(x => _popup.OpenNotification("Failed to refresh recognition module", x.Message, true, true));
    }
    public override void OptionsSelectedModuleRestartClicked()
    {
        if (_recognition.GetCurrentModuleStatus() != ServiceStatus.Stopped)
        {
            _logger.Information("Restarting recognition module");
            var res = _recognition.StopModule();
            res = res.IsOk ? _recognition.StartModule() : res;
            res.IfFail(x => _popup.OpenNotification("Failed to restart recognition module", x.Message, true, true));
        }
    }
    private void OptionsSelectedModuleUpdateButtons(ServiceStatus status, bool listening)
    {
        var running = status != ServiceStatus.Stopped;
        listening = running && listening;

        OptionsSelectedModuleStartStopText = running ? "Running" : "Stopped";
        OptionsSelectedModuleStartStopBrush = running ? _uiHelper.ValidBrush : _uiHelper.InvalidBrush;

        OptionsSelectedModuleToggleMuteText = listening ? "Listening" : "Muted";
        OptionsSelectedModuleToggleMuteBrush = listening ? _uiHelper.ValidBrush : _uiHelper.InvalidBrush;
        OptionsSelectedModuleToggleMuteEnabled = running;

        OptionsSelectedModuleRestartEnabled = running;
    }

    public override void OptionsOutputNoiseFilterClicked()
    {
        _logger.Information("Manually editing recognition noise filter");
        _popup.OpenEditList(Config.Recognition_Fixup_NoiseFilter, "Edit Recognition Noise Filters", "Noise", null, OptionsOutputNoiseFilterRefresh);
    }
    private void OptionsOutputNoiseFilterRefresh()
    {
        var res = _recognition.UpdateSettings();
        res.IfFail(x => _popup.OpenNotification("Failed to update recognition noise filter", x.Message, true, true));
    }

    public override void OptionsMicrophoneChanged()
    {
        var selected = OptionsMicrophone.GetSelected();
        if (selected is null)
        {
            return;
        }

        var mics = OptionsMicrophoneGetNames();
        if (!mics.IsOk)
        {
            _popup.OpenNotification("Failed to assign selected microphone", mics.Msg.Message, true, true);
            return;
        }

        var match = mics.Value.FirstOrDefault(x => x == selected);
        if (match is not null)
        {
            Config.Recognition_MicrophoneName = match;
        }
    }
    public override void OptionsMicrophoneRefreshClicked()
    {
        var mics = OptionsMicrophoneGetNames();
        if (!mics.IsOk)
        {
            _popup.OpenNotification("Failed to retrieve microphones", mics.Msg.Message, true, true);
            return;
        }
        OptionsMicrophone.RefreshItems(mics.Value, Config.Recognition_MicrophoneName);
    }
    private Res<string[]> OptionsMicrophoneGetNames()
    {
        var mics = _audio.GetCaptureDevices();
        return mics.IsOk ? ResC.TOk(mics.Value.Select(x => x.Name).ToArray()) : ResC.TFail<string[]>(mics.Msg);
    }

    public override void ModulesAnyApiEditPresets()
    {
        _logger.Information("Editing api presets");
        _popup.OpenEditApiPresets(Config.Api_Presets, null, ModulesAnyApiReloadPresetBox);
    }
    private void ModulesAnyApiReloadPresetBox()
    {
        _logger.Debug("Reloading Any-API Preset ComboBox");
        var presetNames = Config.Api_Presets.Select(x => x.Name).ToArray();
        ModulesAnyApiPresets.RefreshItems(presetNames, Config.Recognition_Api_Preset);
    }
    public override void ModulesAnyApiPresetChanged()
    {
        var selected = ModulesAnyApiPresets.GetSelected();
        if (selected is null) return;

        var match = Config.Api_Presets.FirstOrDefault(x => x.Name == selected);
        if (match is null)
        {
            _logger.Warning("Failed to find API preset match for value {val}", selected);
            return;
        }

        Config.Recognition_Api_Preset = selected;
    }

    public override void ModulesAzureEditLanguages()
    {
        _logger.Information("Editing azure languages");
        _popup.OpenEditList(Config.Recognition_Azure_Languages, "Edit Azure Languages", "Language", null);
    }

    public override void ModulesAzureEditPresetPhrases()
    {
        _logger.Information("Editing azure phrases");
        _popup.OpenEditList(Config.Recognition_Azure_PresetPhrases, "Edit Azure Preset Phrases", "Phrase", null);
    }

    public override void ModulesVoskEditModels()
    {
        _logger.Information("Editing vosk models");
        _popup.OpenEditDict("Editing Vosk Models", "Model Name", "Model Path", Config.Recognition_Vosk_Models, null, ModulesVoskReloadModelBox);
    }
    private void ModulesVoskReloadModelBox()
    {
        _logger.Debug("Reloading Vosk Model ComboBox");
        ModulesVoskModels.RefreshItems([.. Config.Recognition_Vosk_Models.Keys], Config.Recognition_Vosk_CurrentModel);
    }
    public override void ModulesVoskModelChanged()
    {
        var selected = ModulesVoskModels.GetSelected();
        if (selected is null) return;

        if (Config.Recognition_Vosk_Models.ContainsKey(selected))
        {
            _logger.Warning("Failed to find Vosk Model match for value {val}", selected);
            return;
        }

        Config.Recognition_Vosk_CurrentModel = selected;
    }

    public override void ModulesWhisperEditModels()
    {
        _logger.Information("Editing whisper models");
        _popup.OpenEditDict("Editing Whisper Models", "Model Name", "Model Path", Config.Recognition_Whisper_Models, null, ModulesWhisperReloadModelBox);
    }
    private void ModulesWhisperReloadModelBox()
    {
        _logger.Debug("Reloading Whisper Model ComboBox");
        ModulesWhisperModels.RefreshItems([.. Config.Recognition_Whisper_Models.Keys], Config.Recognition_Whisper_SelectedModel);
    }
    public override void ModulesWhisperModelChanged()
    {
        var selected = ModulesWhisperModels.GetSelected();
        if (selected is null) return;

        if (Config.Recognition_Whisper_Models.ContainsKey(selected))
        {
            _logger.Warning("Failed to find Whisper Model match for value {val}", selected);
            return;
        }

        Config.Recognition_Whisper_SelectedModel = selected;
    }
    public override void ModulesWhisperEditNoiseFilter()
    {
        _logger.Information("Editing whisper noise filters");
        _popup.OpenEditDict("Editing Whisper Noise Filter", "Noise Name", "Noise Text", Config.Recognition_Whisper_Cfg_NoiseFilter, null); //todo: format, reload needed?
    }
    public override void ModulesWhisperVadModeChanged()
    {
        var selected = ModulesWhisperModels.GetSelected();
        if (selected is null) return;

        if (!Enum.TryParse<WhisperIpcVadOperatingMode>(selected, out var match))
        {
            _logger.Warning("Failed to find WhisperIpcVadOperatingMode match for value {val}", selected);
            return;
        }

        Config.Recognition_Whisper_Cfg_VadOperatingMode = match;
    }

    public override void ModulesWindowsModelChanged()
    {
        #if WINDOWS
        ModulesWindowsModelsUpdateComboBox();
        #endif
    }
    #if WINDOWS
    private void ModulesWindowsModelsUpdateComboBox()
    {
        var description =  "Description: ";

        var selected = ModulesWindowsModels.GetSelected();
        if (selected is null)
        {
            description += "No module is selected";
        }
        else
        {
            if (!_windowsModels.TryGetValue(selected, out var modelData))
            {
                description += "Selected module not found";
                Config.Recognition_Windows_ModelId = string.Empty;
            }
            else
            {
                description += modelData.Desc;
                Config.Recognition_Windows_ModelId = modelData.Id;
            }
        }
        ModulesWindowsModelDescription = description;
    }
    #endif
}

#if DEBUG
public class RecogSubMenuViewModelPreview : RecogSubMenuViewModelBase
{
    public RecogSubMenuViewModelPreview()
    {
        Config = new();

        OptionsSelectedModule = new();
        OptionsSelectedModuleStartStopText = "Stopped";
        OptionsSelectedModuleToggleMuteText = "Muted";
        OptionsSelectedModuleRestartNeeded = true;
        OptionsSelectedModuleDescription = "Description Placeholder 123";

        OptionsMicrophoneAvailable = true;
        OptionsMicrophone = new();

        ModulesAnyApiIsSelected = true;
        ModulesAnyApiPresets = new();

        ModulesAzureIsSelected = true;

        ModulesVoskIsSelected = true;
        ModulesVoskModels = new();

        ModulesWhisperIsSelected = true;
        ModulesWhisperModels = new();
        ModulesWhisperVadMode = new();
        ModulesWhisperShowAdvancedSettings = true;

        ModulesWindowsIsSelected = true;
        ModulesWindowsModels = new();
        ModulesWindowsModelDescription = "Sample Description";
    }
}
#endif