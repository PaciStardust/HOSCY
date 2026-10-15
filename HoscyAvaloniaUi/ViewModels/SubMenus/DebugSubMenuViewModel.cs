using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;
using Serilog.Events;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class DebugSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    [ObservableProperty]
    public partial AudioDeviceComboBoxData InfoSpeaker { get; set; }
    [ObservableProperty]
    public partial string InfoSpeakerStatusText { get; set; }
    [ObservableProperty]
    public partial string InfoSpeakerVolumeText { get; set; }
    [ObservableProperty]
    public partial bool InfoSpeakerApplyNeeded { get; protected set; }
    public virtual void InfoSpeakerChanged() { }
    public virtual void InfoSpeakerFallbackChanged() { }
    public virtual void InfoSpeakerRefreshClicked() { }
    public virtual void InfoSpeakerApplyClicked() { }
    public virtual void InfoSpeakerVolumeChanged() { }


    [ObservableProperty]
    public partial ComboBoxData LogLevels { get; set; }

    [ObservableProperty]
    public partial string LogFiltersInvalid { get; set; } = string.Empty;
    public virtual void LogLevelChanged() { }
    public virtual void LogFiltersClicked() { }
    
    public virtual void UtilOpenGit() { }
    public virtual void UtilOpenConfig() { }
    public virtual void UtilSaveConfig() { }
    public virtual void UtilManageServices() { }
}

[PrototypeLoadIntoDiContainer(typeof(DebugSubMenuViewModelBase), Lifetime.Transient)]
public class DebugSubMenuViewModelImpl : DebugSubMenuViewModelBase
{
    private readonly PopupWindowFactory _popup;
    private readonly ILogger _logger;
    private readonly IAudioService _audio;
    private readonly IApplicationSound _applicationSound;

    public DebugSubMenuViewModelImpl
    (
        ILogger logger, 
        ConfigModel config, 
        PopupWindowFactory popupFactory, 
        IAudioService audio,
        IBackToFrontNotifyService notify,
        IApplicationSound applicationSound
    )
    {
        Config = config;
        _logger = logger.ForContext<DebugSubMenuViewModelImpl>();
        _popup = popupFactory;
        _audio = audio;
        _applicationSound = applicationSound;

        List<ResMsg> errors = [];
        InfoSpeaker = new(_logger, _audio, "InfoSpeaker", false, notify, 
            Config.Debug_InfoNoiseSpeakerName, Config.Debug_InfoNoiseSpeakerFallbackName);
        InfoSpeakerStatusText = _applicationSound.GetPlaybackStatus().ToString();
        _applicationSound.OnPlaybackStatusChanged += InfoNoiseSpeakerStatusChanged;

        LogLevels = new(Enum.GetNames<LogEventLevel>(), Enum.GetName(Config.Debug_LogMinimumSeverity) ?? string.Empty, _logger, "LogLevel");
        LogUpdateFilterValidity();

        if (errors.Count > 0)
        {
            var error = ResC.FailM(errors);
            notify.SendResult("Some Data Could Not be Loaded", error.Msg!);
        }
    }

    public override void InfoSpeakerChanged()
    {
        if (InfoSpeaker.OnSelectionChanged(Config.Debug_InfoNoiseSpeakerName, false, out var newSpeaker))
        {
            Config.Debug_InfoNoiseSpeakerName = newSpeaker;
        }
    }
    public override void InfoSpeakerFallbackChanged()
    {
        if (InfoSpeaker.OnSelectionChanged(Config.Debug_InfoNoiseSpeakerFallbackName, true, out var newSpeaker))
        {
            Config.Debug_InfoNoiseSpeakerFallbackName = newSpeaker;
        }
    }

    public override void InfoSpeakerRefreshClicked()
    {
        InfoSpeaker.RefreshItems(Config.Debug_InfoNoiseSpeakerName, Config.Debug_InfoNoiseSpeakerFallbackName);
    }
    public override void InfoSpeakerApplyClicked()
    {
        var res = _applicationSound.Refresh();;
        res.IfFail(x => _popup.OpenNotification("Failed to Apply Speaker", x.Message, true, true));
    }
    public override void InfoSpeakerVolumeChanged()
    {
        InfoSpeakerVolumeText = $"Speaker Volume ({MathF.Round(Config.Debug_InfoNoiseVolumePercent * 100)}%)";
    }

    private void InfoNoiseSpeakerStatusChanged(AudioPlaybackDeviceStatusInfo info)
    {
        InfoSpeakerStatusText = info.ToString();
    }

    public override void LogLevelChanged()
    {
        var selected = LogLevels.GetSelected();
        if (selected is null) return;

        if (!Enum.TryParse<LogEventLevel>(selected, out var parsed))
        {
            _logger.Warning("Failed to parse drop down value {selected} to LogLevel", selected);
            return;
        }
        Config.Debug_LogMinimumSeverity = parsed;
    }

    public override void LogFiltersClicked()
    {
        _popup.OpenEditFilters(Config.Debug_LogFilters, null, LogFiltersClosed);
    }
    private void LogFiltersClosed()
    {
        var strings = LogUpdateFilterValidity();
        if (strings.Length > 0)
        {
            var msg = $"Following filters are invalid:\n{string.Join("\n", strings.Select(x => $" - {x}"))}";
            _popup.OpenNotification("Invalid Filters Found", msg, false, true);
        }
        Config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
    }
    private string[] LogUpdateFilterValidity()
    {
        var invalidFilters = Config.Debug_LogFilters.Where(x => !x.IsValid);
        if (!invalidFilters.Any())
        {
            LogFiltersInvalid = string.Empty;
            return [];
        }

        var strings = invalidFilters.Select(x => x.Name).ToArray();
        LogFiltersInvalid = $"({strings.Length} Filter{(strings.Length == 1 ? "" : "s")} Invalid)";
        return strings;
    }

    public override void UtilOpenConfig()
    {
        _logger.Information("Manually opening config");
        OtherUtils.OpenFileOrFolder(PathUtils.PathConfigFolder, _logger);
    }
    public override void UtilOpenGit()
    {
        _logger.Information("Manually opening git");
        OtherUtils.OpenGithub(_logger);
    }
    public override void UtilSaveConfig()
    {
        _logger.Information("Manually saving config");
        Config.TrySave(PathUtils.PathConfigFolder,ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
    }
    public override void UtilManageServices()
    {
        _logger.Information("Opening service manager");
        _popup.OpenServiceManager(null);
    }
}

#if DEBUG
public class DebugSubMenuViewModelPreview : DebugSubMenuViewModelBase
{
    public DebugSubMenuViewModelPreview()
    {
        Config = new()
        {
            Debug_LogViaFileFollow = true
        };
        LogLevels = new(["Test"], string.Empty, null, string.Empty);
        LogFiltersInvalid = "(n Filters Invalid)";
    }
}
#endif