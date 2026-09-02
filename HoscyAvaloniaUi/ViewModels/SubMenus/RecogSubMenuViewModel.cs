using System;
using System.Linq;
using Avalonia;
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

        var mics = OptionsMicrophoneGetNames();
        mics.IfFail(x => notify.SendResult("Failed loading playback devices", x));
        OptionsMicrophone = new(mics.Value ?? [], Config.Recognition_MicrophoneName, _logger, "OptionsMicrophone");
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
    }
    private void OptionsSelectedModuleOnStatusChanged(object? sender, RecognitionStatusChangedEventArgs e)
    {
        OptionsSelectedModuleUpdateButtons(e.Status, e.IsListening); //todo: sound?
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
    }
}
#endif