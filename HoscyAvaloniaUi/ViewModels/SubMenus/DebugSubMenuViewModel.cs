using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.Utility;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;
using Serilog.Events;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class DebugSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    [ObservableProperty]
    public partial string[] LogLevels { get; protected set; } = [ "Test" ];
    [ObservableProperty]
    public partial int LogLevelIndex { get; set; }
    
    public virtual void LogLevelChanged() { }
    public virtual void LogFiltersClicked() { }
    public virtual void UtilOpenGit() { }
    public virtual void UtilOpenConfig() { }
    public virtual void UtilSaveConfig() { }
    public virtual void UtilReloadDevices() { }
}

[PrototypeLoadIntoDiContainer(typeof(DebugSubMenuViewModelBase), Lifetime.Transient)]
public class DebugSubMenuViewModelImpl : DebugSubMenuViewModelBase
{
    private readonly PopupWindowFactory _popupFactory;
    private readonly ILogger _logger;
    private readonly IAudioService _audio;

    public DebugSubMenuViewModelImpl(ILogger logger, ConfigModel config, PopupWindowFactory popupFactory, IAudioService audio)
    {
        Config = config;
        _logger = logger.ForContext<DebugSubMenuViewModelImpl>();
        _popupFactory = popupFactory;
        _audio = audio;

        (LogLevels, LogLevelIndex) = AvaloniaUiUtils.ComboBoxLoad(Enum.GetNames<LogEventLevel>(), 
            Enum.GetName(Config.Debug_LogMinimumSeverity), _logger, "LogLevel");
    }

    public override void LogLevelChanged()
    {
        (var selected, LogLevelIndex) = AvaloniaUiUtils.ComboBoxIsValid(LogLevels, LogLevelIndex, _logger, "LogLevel");
        LogLevelIndex = Math.Min(LogLevelIndex, LogLevels.Length - 1);

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
        _popupFactory.OpenEditFilters(Config.Debug_LogFilters, null,
            () => Config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _logger));
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
    public override void UtilReloadDevices()
    {
        _logger.Information("Reloading audio devices");
        var res1 = _audio.GetCaptureDevices();
        if (!res1.IsOk)
        {
            _popupFactory.OpenNotification("Can not load capture devices", res1.Msg.Message, true, true);
            return;
        }

        var res2 = _audio.GetPlaybackDevices();
        if (!res2.IsOk)
        {
            _popupFactory.OpenNotification("Can not load playback devices", res2.Msg.Message, true, true);
            return;
        }
        _logger.Information("Reloaded audio devices");
    }
}

#if DEBUG
public class DebugSubMenuViewModelPreview : DebugSubMenuViewModelBase
{
    public DebugSubMenuViewModelPreview()
    {
        Config = new();
    }
}
#endif