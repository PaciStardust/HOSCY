using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HoscyAvaloniaUi.Services;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;
using Serilog.Events;

namespace HoscyAvaloniaUi.ViewModels.SubMenus;

public abstract partial class DebugSubMenuViewModelBase : ViewModelBase
{
    [ObservableProperty]
    public partial ConfigModel Config { get; set; }

    public string[] LogLevels { get; protected set; } = [ "Test" ];
    public int LogLevelIndex { get; set; }
    public virtual void LogLevelChanged() { }
    public virtual void LogFiltersClicked() { }
}

[PrototypeLoadIntoDiContainer(typeof(DebugSubMenuViewModelBase), Lifetime.Transient)]
public class DebugSubMenuViewModelImpl : DebugSubMenuViewModelBase
{
    private readonly PopupWindowFactory _popupFactory;
    private readonly ILogger _logger;

    public DebugSubMenuViewModelImpl(ILogger logger, ConfigModel config, PopupWindowFactory popupFactory)
    {
        Config = config;
        _logger = logger.ForContext<DebugSubMenuViewModelImpl>();
        _popupFactory = popupFactory;

        LogLevels = Enum.GetNames<LogEventLevel>();
        LogLevelIndex = LogLevels.IndexOf(Enum.GetName(Config.Debug_LogMinimumSeverity)); //todo: log
    }

    public override void LogLevelChanged() //todo: log
    {
        LogLevelIndex = Math.Min(LogLevelIndex, LogLevels.Length - 1);
        if (LogLevelIndex == -1)
        {
            return;
        }
        var selected = LogLevels[LogLevelIndex];
        if (Enum.TryParse<LogEventLevel>(selected, out var parsed))
        {
            Config.Debug_LogMinimumSeverity = parsed;
        }
    }

    public override void LogFiltersClicked()
    {
        _popupFactory.OpenEditFilters(Config.Debug_LogFilters, null);
        Config.TrySave(PathUtils.PathConfigFolder, ConfigModelLoader.DEFAULT_FILE_NAME, _logger);
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