using System;
using System.Linq;
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
    public partial ComboBoxData LogLevels { get; set; }

    [ObservableProperty]
    public partial string LogFiltersInvalid { get; set; } = string.Empty;
    
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

        LogLevels = new(Enum.GetNames<LogEventLevel>(), Enum.GetName(Config.Debug_LogMinimumSeverity) ?? string.Empty, _logger, "LogLevel");
        LogUpdateFilterValidity();
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
        _popupFactory.OpenEditFilters(Config.Debug_LogFilters, null, LogFiltersClosed);
    }
    private void LogFiltersClosed()
    {
        var strings = LogUpdateFilterValidity();
        if (strings.Length > 0)
        {
            var msg = $"Following filters are invalid:\n{string.Join("\n", strings.Select(x => $" - {x}"))}";
            _popupFactory.OpenNotification("Invalid filters found", msg, false, true);
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
        Config = new()
        {
            Debug_LogViaFileFollow = true
        };
        LogLevels = new(["Test"], string.Empty, null, string.Empty);
        LogFiltersInvalid = "(n filters invalid)";
    }
}
#endif