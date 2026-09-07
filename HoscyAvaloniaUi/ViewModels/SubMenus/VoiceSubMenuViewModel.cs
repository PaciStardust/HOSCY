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
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class VoiceSubMenuViewModelBase : ViewModelBase
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

    [ObservableProperty]
    public partial ComboBoxData OptionsSpeaker { get; set; }
    [ObservableProperty]
    public partial string OptionsSpeakerVolumeText { get; set; }
    public virtual void OptionsSpeakerChanged() { }
    public virtual void OptionsSpeakerRefreshClicked() { }
    public virtual void OptionsSpeakerVolumeChanged() { }

    [ObservableProperty]
    public partial string ModulesSettingsVisibleIfCompatible { get; protected set; } = "(Settings are visible if compatible voice module is selected)";

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

    public VoiceSubMenuViewModelImpl //todo: windows module
    (
        ConfigModel config, 
        ILogger logger, 
        IAudioService audio, 
        IBackToFrontNotifyService notify,
        PopupWindowFactory popup,
        IVoiceManagerService voice,
        UiHelperService uiHelper
    )
    {
        Config = config;
        _logger = logger.ForContext<VoiceSubMenuViewModelImpl>();
        _audio = audio;
        _popup = popup;
        _voice = voice;
        _uiHelper = uiHelper;

        OptionsSelectedModuleUpdateButtons(_voice.GetCurrentModuleStatus());
        _voice.OnModuleStatusChanged += OptionsSelectedModuleOnStatusChanged;

        _voiceInfosOrdered = [.. _voice.GetModuleInfos().OrderByDescending(x => x.Priority)];
        OptionsSelectedModule = new([.. _voiceInfosOrdered.Select(x => x.Name)], Config.Voice_SelectedModuleName, _logger, "OptionsSelectedModule");
        OptionsSelectedModuleUpdateComboBox();

        List<ResMsg> errors = [];
        var speakers = OptionsSpeakerGetNames();
        speakers.IfFail(errors.Add);
        OptionsSpeaker = new(speakers.Value ?? [], Config.Voice_CurrentSpeakerName, _logger, "OptionsSpeaker");

        ModulesAnyApiPresets = new([.. Config.Api_Presets.Select(x => x.Name)], Config.Voice_Api_Preset, _logger, "ModulesAnyApiPresets");

        ModulesAzureVoices = new([.. Config.Voice_Azure_VoiceList.Select(x => x.Name)], Config.Voice_Azure_CurrentVoice, _logger, "ModulesAzureVoices");

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
        var flags = VoiceModuleConfigFlags.None;

        var selected = OptionsSelectedModule.GetSelected();
        if (selected is null)
        {
            description += "No module is selected";
        }
        else
        {
            var match = _voiceInfosOrdered.FirstOrDefault(x => x.Name == selected);
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

        Config.Voice_SelectedModuleName = selected ?? string.Empty;

        OptionsSelectedModuleDescription = description;

        ModulesAnyApiIsSelected = flags.HasFlag(VoiceModuleConfigFlags.AnyApi);
        ModulesAzureIsSelected = flags.HasFlag(VoiceModuleConfigFlags.Azure);
        ModulesPiperIsSelected = flags.HasFlag(VoiceModuleConfigFlags.PiperWeb);
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
            res.IfFail(x => _popup.OpenNotification("Failed to start voice module", x.Message, true, true));
        } 
        else
        {
            _logger.Information("Stopping voice module");
            OptionsSelectedModuleStartStopText = "Stopping";
            var res = _voice.StopModule();
            res.IfFail(x => _popup.OpenNotification("Failed to stop voice module", x.Message, true, true));
        }
    }
    public override void OptionsSelectedModuleRefreshClicked()
    {
        _logger.Information("Refreshing voice module");
        var res = _voice.RefreshModule();
        res.IfFail(x => _popup.OpenNotification("Failed to refresh voice module", x.Message, true, true));
    }
    public override void OptionsSelectedModuleRestartClicked()
    {
        if (_voice.GetCurrentModuleStatus() != ServiceStatus.Stopped)
        {
            _logger.Information("Restarting voice module");
            var res = _voice.StopModule();
            res = res.IsOk ? _voice.StartModule() : res;
            res.IfFail(x => _popup.OpenNotification("Failed to restart voice module", x.Message, true, true));
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
            _popup.OpenNotification("Failed to assign selected speaker", speakers.Msg.Message, true, true);
            return;
        }

        var match = speakers.Value.FirstOrDefault(x => x == selected);
        if (match is not null)
        {
            Config.Voice_CurrentSpeakerName = match;
        }
    }
    public override void OptionsSpeakerRefreshClicked()
    {
        var speakers = OptionsSpeakerGetNames();
        if (!speakers.IsOk)
        {
            _popup.OpenNotification("Failed to retrieve speakers", speakers.Msg.Message, true, true);
            return;
        }
        OptionsSpeaker.RefreshItems(speakers.Value, Config.Voice_CurrentSpeakerName);
    }
    private Res<string[]> OptionsSpeakerGetNames()
    {
        var speakers = _audio.GetPlaybackDevices();
        return speakers.IsOk ? ResC.TOk(speakers.Value.Select(x => x.Name).ToArray()) : ResC.TFail<string[]>(speakers.Msg);
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
        _logger.Debug("Reloading Any-API Preset ComboBox");
        var presetNames = Config.Api_Presets.Select(x => x.Name).ToArray();
        ModulesAnyApiPresets.RefreshItems(presetNames, Config.Voice_Api_Preset);
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
    }
    public override void ModulesAzureVoiceChanged()
    {
        var selected = ModulesAzureVoices.GetSelected();
        if (selected is null) return;

        var match = Config.Voice_Azure_VoiceList.FirstOrDefault(x => x.Name == selected);
        if (match is null)
        {
            _logger.Warning("Failed to find Azure Voice match for value {val}", selected);
            return;
        }

        Config.Voice_Azure_CurrentVoice = selected;
    }
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
    }
}
#endif