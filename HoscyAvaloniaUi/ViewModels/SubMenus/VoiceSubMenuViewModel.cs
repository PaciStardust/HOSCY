using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.Services.ChangeTracking;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class VoiceSubMenuViewModelBase : ViewModelBaseWithLoadedIndicator
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
    public partial bool OptionsSelectedModuleRestartEnabled { get; protected set; }
    [ObservableProperty]
    public partial bool OptionsSelectedModuleRestartNeeded { get; protected set; }
    public virtual void OptionsSelectedModuleChanged() { }
    public virtual void OptionsSelectedModuleStartStopClicked() { }
    public virtual void OptionsSelectedModuleRefreshClicked() { }
    public virtual void OptionsSelectedModuleRestartClicked() { }
    public virtual void OptionsSelectedModuleSetUnappliedChange() { }

    [ObservableProperty]
    public partial ComboBoxData OptionsSpeaker { get; set; }
    [ObservableProperty]
    public partial string OptionsSpeakerVolumeText { get; set; }
    [ObservableProperty]
    public partial bool OptionsSpeakerApplyNeeded { get; protected set; }
    public virtual void OptionsSpeakerChanged() { }
    public virtual void OptionsSpeakerRefreshClicked() { }
    public virtual void OptionsSpeakerApplyClicked() { }
    public virtual void OptionsSpeakerVolumeChanged() { }

    [ObservableProperty]
    public partial string ModulesSettingsVisibleIfCompatible { get; protected set; } = "(Settings are Visible if Compatible Voice Module is Selected)";

    [ObservableProperty]
    public partial bool ModulesAnyApiIsSelected { get; protected set; }
    [ObservableProperty]
    public partial ComboBoxData ModulesAnyApiPresets { get; set; }
    public virtual void ModulesAnyApiEditPresets() { }
    public virtual void ModulesAnyApiPresetChanged() { }

    [ObservableProperty]
    public partial bool ModulesAzureIsSelected { get; protected set; }
    [ObservableProperty]
    public partial ComboBoxData ModulesAzureVoices { get; set; }
    public virtual void ModulesAzureEditVoices() { }
    public virtual void ModulesAzureVoiceChanged() { }

    [ObservableProperty]
    public partial bool ModulesPiperIsSelected { get; protected set; }

    [ObservableProperty]
    public partial bool ModulesWindowsIsSelected { get; protected set; }
    [ObservableProperty]
    public partial ComboBoxData ModulesWindowsModels { get; set; }
    [ObservableProperty]
    public partial string ModulesWindowsModelDescription { get; set; }
    public virtual void ModulesWindowsModelChanged() { }
}

[PrototypeLoadIntoDiContainer(typeof(VoiceSubMenuViewModelBase), Lifetime.Transient)]
public class VoiceSubMenuViewModelImpl : VoiceSubMenuViewModelBase
{
    private readonly ILogger _logger;
    private readonly IAudioService _audio;
    private readonly PopupWindowFactory _popup;
    private readonly IVoiceManagerService _voice;
    private readonly IVoiceModuleStartInfo[] _voiceInfosOrdered;
    private readonly UiHelperService _uiHelper;
    private readonly VoiceUnappliedTracker _unapplied;

    #if WINDOWS
    private readonly Dictionary<string,(string Desc,string Id)> _windowsModels;
    #endif

    public VoiceSubMenuViewModelImpl
    (
        ConfigModel config, 
        ILogger logger, 
        IAudioService audio, 
        IBackToFrontNotifyService notify,
        PopupWindowFactory popup,
        IVoiceManagerService voice,
        UiHelperService uiHelper,
        VoiceUnappliedTracker unapplied
    )
    {
        Config = config;
        _logger = logger.ForContext<VoiceSubMenuViewModelImpl>();
        _audio = audio;
        _popup = popup;
        _voice = voice;
        _uiHelper = uiHelper;
        _unapplied = unapplied;

        OptionsSelectedModuleUpdateButtons(_voice.GetCurrentModuleStatus());
        _voice.OnModuleStatusChanged += OptionsSelectedModuleOnStatusChanged;

        _voiceInfosOrdered = [.. _voice.GetModuleInfos().OrderByDescending(x => x.Priority)];
        OptionsSelectedModule = new([.. _voiceInfosOrdered.Select(x => x.Name)], Config.Voice_SelectedModuleName, _logger, "OptionsSelectedModule");
        OptionsSelectedModuleUpdateComboBox();

        List<ResMsg> errors = [];
        var speakers = OptionsSpeakerGetNames();
        speakers.IfFail(errors.Add);
        OptionsSpeaker = new(speakers.Value ?? [], Config.Voice_CurrentSpeakerName, _logger, "OptionsSpeaker");
        OptionsSpeakerUpdateApplyNeeded();

        ModulesAnyApiPresets = new([.. Config.Api_Presets.Select(x => x.Name)], Config.Voice_Api_Preset, _logger, "ModulesAnyApiPresets");

        ModulesAzureVoices = new([.. Config.Voice_Azure_VoiceList.Select(x => x.Name)], Config.Voice_Azure_CurrentVoice, _logger, "ModulesAzureVoices");

        #if WINDOWS
        _windowsModels = [];
        var winModels = WinApi.GetWindowsVoices(_logger);
        winModels.IfFail(errors.Add);
        foreach(var model in winModels.Value ?? [])
        {
            _windowsModels[model.Name] = (model.Description, model.Id);
        }
        ModulesWindowsModels = new([.. _windowsModels.Keys], _windowsModels.FirstOrDefault(x => x.Value.Id == Config.Voice_Windows_ModelName).Key, _logger, "ModulesWindowsModels");
        ModulesWindowsModelsUpdateComboBox();
        #else
        ModulesWindowsModels = new();
        ModulesWindowsModelDescription = "This Feature is Not Supported Outside of Windows";
        #endif

        if (errors.Count > 0)
        {
            var error = ResC.FailM(errors);
            notify.SendResult("Some Data Could Not be Loaded", error.Msg!);
        }

        _unapplied.OnUnappliedChanged += OptionsSelectedModuleOnUnappliedChanged;
        OptionsSelectedModuleOnUnappliedChanged(_unapplied.Unapplied);
    }
    private void OptionsSelectedModuleOnUnappliedChanged(bool obj)
    {
        OptionsSelectedModuleRestartNeeded = obj;
    }
    public override void OptionsSelectedModuleSetUnappliedChange()
    {
        if (Loaded)
        {
            _unapplied.SetChange();
        }
    }

    public override void OptionsSelectedModuleChanged()
    {
        OptionsSelectedModuleUpdateComboBox();
    }
    private void OptionsSelectedModuleUpdateComboBox()
    {
        var description =  "Description: ";
        var flags = VoiceModuleConfigFlags.None;

        var selected = OptionsSelectedModule.GetSelected();
        if (selected is null)
        {
            description += "No Module is Selected";
        }
        else
        {
            OptionsSelectedModuleSetUnappliedChange();
            var match = _voiceInfosOrdered.FirstOrDefault(x => x.Name == selected);
            if (match is null)
            {
                description += "Selected Module Not Found";
            }
            else
            {
                description += match.Description;
                flags = match.ConfigFlags;
            }
        }

        Config.Voice_SelectedModuleName = selected ?? string.Empty;

        OptionsSelectedModuleDescription = description;

        ModulesAnyApiIsSelected = flags.HasFlag(VoiceModuleConfigFlags.AnyApi);
        ModulesAzureIsSelected = flags.HasFlag(VoiceModuleConfigFlags.Azure);
        ModulesPiperIsSelected = flags.HasFlag(VoiceModuleConfigFlags.PiperWeb);
        ModulesWindowsIsSelected = flags.HasFlag(VoiceModuleConfigFlags.Windows);
    }
    private void OptionsSelectedModuleOnStatusChanged(ServiceStatus status)
    {
        OptionsSelectedModuleUpdateButtons(status);
    }
    public override void OptionsSelectedModuleStartStopClicked()
    {
        if (_voice.GetCurrentModuleStatus() == ServiceStatus.Stopped)
        {
            _logger.Information("Starting voice module");
            OptionsSelectedModuleStartStopText = "Starting";
            var res = _voice.StartModule();
            res.IfFail(x => _popup.OpenNotification("Failed to Start Voice Module", x.Message, true, true));
        } 
        else
        {
            _logger.Information("Stopping voice module");
            OptionsSelectedModuleStartStopText = "Stopping";
            var res = _voice.StopModule();
            res.IfFail(x => _popup.OpenNotification("Failed to Stop Voice Module", x.Message, true, true));
        }
    }
    public override void OptionsSelectedModuleRefreshClicked()
    {
        _logger.Information("Refreshing voice module");
        var res = _voice.RefreshModule();
        res.IfFail(x => _popup.OpenNotification("Failed to Refresh Voice Module", x.Message, true, true));
    }
    public override void OptionsSelectedModuleRestartClicked()
    {
        if (_voice.GetCurrentModuleStatus() != ServiceStatus.Stopped)
        {
            _logger.Information("Restarting voice module");
            var res = _voice.StopModule();
            res = res.IsOk ? _voice.StartModule() : res;
            res.IfFail(x => _popup.OpenNotification("Failed to Restart Voice Module", x.Message, true, true));
        }
    }
    private void OptionsSelectedModuleUpdateButtons(ServiceStatus status)
    {
        var running = status != ServiceStatus.Stopped;

        OptionsSelectedModuleStartStopText = running ? "Running" : "Stopped";
        OptionsSelectedModuleStartStopBrush = running ? _uiHelper.ValidBrush : _uiHelper.InvalidBrush;

        OptionsSelectedModuleRestartEnabled = running;
    }

    public override void OptionsSpeakerChanged()
    {
        var selected = OptionsSpeaker.GetSelected();
        if (selected is null)
        {
            return;
        }

        var speakers = OptionsSpeakerGetNames();
        if (!speakers.IsOk)
        {
            _popup.OpenNotification("Failed to Assign Selected Speaker", speakers.Msg.Message, true, true);
            return;
        }

        var match = speakers.Value.FirstOrDefault(x => x == selected);
        if (match is not null)
        {
            Config.Voice_CurrentSpeakerName = match;
        }
        OptionsSpeakerUpdateApplyNeeded();
    }
    public override void OptionsSpeakerRefreshClicked()
    {
        var speakers = OptionsSpeakerGetNames();
        if (!speakers.IsOk)
        {
            _popup.OpenNotification("Failed to Retrieve Speakers", speakers.Msg.Message, true, true);
            return;
        }
        OptionsSpeaker.RefreshItems(speakers.Value, Config.Voice_CurrentSpeakerName);
        OptionsSpeakerUpdateApplyNeeded();
    }
    private Res<string[]> OptionsSpeakerGetNames()
    {
        var speakers = _audio.GetPlaybackDevices();
        return speakers.IsOk ? ResC.TOk(speakers.Value.Select(x => x.Name).ToArray()) : ResC.TFail<string[]>(speakers.Msg);
    }
    public override void OptionsSpeakerApplyClicked()
    {
        var res = _voice.ChangePlayback(Config.Voice_CurrentSpeakerName);
        res.IfFail(x => _popup.OpenNotification("Failed to Apply Speaker", x.Message, true, true));
        OptionsSpeakerUpdateApplyNeeded();
    }
    private void OptionsSpeakerUpdateApplyNeeded()
    {
        var current = _voice.GetPlaybackName();
        if (current is null)
        {
            OptionsSpeakerApplyNeeded = true;
            return;
        }
        OptionsSpeakerApplyNeeded = current != Config.Voice_CurrentSpeakerName;
    }
    public override void OptionsSpeakerVolumeChanged()
    {
        OptionsSpeakerVolumeText = $"Speaker Volume ({MathF.Round(Config.Voice_AudioVolumePercent * 100)}%)";
    }

    public override void ModulesAnyApiEditPresets()
    {
        _logger.Information("Editing api presets");
        _popup.OpenEditApiPresets(Config.Api_Presets, null, ModulesAnyApiReloadPresetBox);
    }
    private void ModulesAnyApiReloadPresetBox()
    {
        _logger.Debug("Reloading Any-API Preset ComboBox"); //todo: why debug, wrong unapplied settings text, audio not working, unmute during mute issues
        var presetNames = Config.Api_Presets.Select(x => x.Name).ToArray();
        ModulesAnyApiPresets.RefreshItems(presetNames, Config.Voice_Api_Preset);
        OptionsSelectedModuleSetUnappliedChange();
    }
    public override void ModulesAnyApiPresetChanged()
    {
        var selected = ModulesAnyApiPresets.GetSelected();
        if (selected is null) return;

        OptionsSelectedModuleSetUnappliedChange();
        var match = Config.Api_Presets.FirstOrDefault(x => x.Name == selected);
        if (match is null)
        {
            _logger.Warning("Failed to find API preset match for value {val}", selected);
            return;
        }

        Config.Voice_Api_Preset = selected;
    }

    public override void ModulesAzureEditVoices()
    {
        _logger.Information("Editing azure voices");
        _popup.OpenEditAzureVoices(Config.Voice_Azure_VoiceList, null, ModulesAzureReloadVoicesBox);
    }
    private void ModulesAzureReloadVoicesBox()
    {
        _logger.Debug("Reloading Azure Voices ComboBox");
        var voiceNames = Config.Voice_Azure_VoiceList.Select(x => x.Name).ToArray();
        ModulesAzureVoices.RefreshItems(voiceNames, Config.Voice_Azure_CurrentVoice);
        OptionsSelectedModuleSetUnappliedChange();
    }
    public override void ModulesAzureVoiceChanged()
    {
        var selected = ModulesAzureVoices.GetSelected();
        if (selected is null) return;

        OptionsSelectedModuleSetUnappliedChange();
        var match = Config.Voice_Azure_VoiceList.FirstOrDefault(x => x.Name == selected);
        if (match is null)
        {
            _logger.Warning("Failed to find Azure Voice match for value {val}", selected);
            return;
        }

        Config.Voice_Azure_CurrentVoice = selected;
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
            description += "No Module is Selected";
        }
        else
        {
            OptionsSelectedModuleSetUnappliedChange();
            if (!_windowsModels.TryGetValue(selected, out var modelData))
            {
                description += "Selected Module Not Found";
                Config.Voice_Windows_ModelName = string.Empty;
            }
            else
            {
                description += modelData.Desc;
                Config.Voice_Windows_ModelName = modelData.Id;
            }
        }
        ModulesWindowsModelDescription = description;
    }
    #endif
}

#if DEBUG
public class VoiceSubMenuViewModelPreview : VoiceSubMenuViewModelBase
{
    public VoiceSubMenuViewModelPreview()
    {
        Config = new()
        {
            Voice_Piper_Process_Enabled = true
        };

        OptionsSelectedModule = new();
        OptionsSelectedModuleStartStopText = "Stopped";
        OptionsSelectedModuleRestartNeeded = true;
        OptionsSelectedModuleDescription = "Description Placeholder 123";

        OptionsSpeaker = new();
        OptionsSpeakerVolumeText = "Speaker Volume";

        ModulesAnyApiIsSelected = true;
        ModulesAnyApiPresets = new();

        ModulesAzureIsSelected = true;
        ModulesAzureVoices = new();

        ModulesPiperIsSelected = true;

        ModulesWindowsIsSelected = true;
    }
}
#endif