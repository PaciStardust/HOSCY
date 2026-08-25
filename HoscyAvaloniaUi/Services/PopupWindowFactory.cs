using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using HoscyAvaloniaUi.ViewModels.Core;
using HoscyAvaloniaUi.ViewModels.Windows;
using HoscyAvaloniaUi.Views.Windows;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.Services;

[LoadIntoDiContainer(typeof(PopupWindowFactory), Lifetime.Singleton)]
public class PopupWindowFactory
(
    ILogger logger,
    UiHelperService uiHelper,
    IContainerBulkLoader<IApplicationSound> soundLoader,
    IContainerBulkLoader<NotificationWindowViewModelBase> notificationWvmLoader,
    IContainerBulkLoader<EditDictWindowViewModelBase> editDictWvmLoader,
    IContainerBulkLoader<EditApiPresetsWindowViewModelBase> editApiPresetsWvmLoader,
    IContainerBulkLoader<EditCountersWindowViewModelBase> editCountersWvmLoader,
    IContainerBulkLoader<EditFiltersWindowViewModelBase> editFiltersWvmLoader,
    IContainerBulkLoader<DisplayListWindowViewModelBase> displayListWvmLoader,
    IContainerBulkLoader<EditListWindowViewModelBase> editListWvmLoader,
    IContainerBulkLoader<EditOscRelayFiltersWindowViewModelBase> editOscRelayFiltersWvmLoader,
    IContainerBulkLoader<EditReplacementsWindowViewModelBase> editReplacementsWvmLoader
)
    : IService
{
    private readonly ILogger _logger = logger.ForContext<PopupWindowFactory>();
    private readonly IApplicationSound? _sound = soundLoader.GetInstance().Value;
    private readonly UiHelperService _uiHelper = uiHelper;

    private readonly IContainerBulkLoader<NotificationWindowViewModelBase> _notificationWvmLoader = notificationWvmLoader;
    private readonly IContainerBulkLoader<EditDictWindowViewModelBase> _editDictWvmLoader = editDictWvmLoader;
    private readonly IContainerBulkLoader<EditApiPresetsWindowViewModelBase> _editApiPresetsWvmLoader = editApiPresetsWvmLoader;
    private readonly IContainerBulkLoader<EditCountersWindowViewModelBase> _editCountersWvmLoader = editCountersWvmLoader;
    private readonly IContainerBulkLoader<EditFiltersWindowViewModelBase> _editFiltersWvmLoader = editFiltersWvmLoader;
    private readonly IContainerBulkLoader<DisplayListWindowViewModelBase> _displayListWvmLoader = displayListWvmLoader;
    private readonly IContainerBulkLoader<EditListWindowViewModelBase> _editListWvmLoader = editListWvmLoader;
    private readonly IContainerBulkLoader<EditOscRelayFiltersWindowViewModelBase> _editOscRelayFiltersWvmLoader = editOscRelayFiltersWvmLoader;
    private readonly IContainerBulkLoader<EditReplacementsWindowViewModelBase> _editReplacementsWvmLoader = editReplacementsWvmLoader;

    private void Open(Func<Window> windowCreate, ViewModelBase vm, bool dialog, Window? parent, Action? onClose)
    {
        try
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                var window = windowCreate();
                if (onClose != null)
                {
                    window.Closed += (_, __) => onClose();
                }

                window.DataContext = vm;
                if (!dialog)
                {
                    _logger.Debug("Showing window {window}", window.GetType().Name);
                    window.Show();
                }
                else
                {
                    if (parent is not null)
                    {
                        _logger.Debug("Showing window {window} as dialog for window {parent}", window.GetType().Name, parent.GetType().Name);
                        window.ShowDialog(parent);
                    }
                    else
                    {
                        _logger.Debug("Showing window {window} as dialog for main window", window.GetType().Name);
                        _uiHelper.ShowDialog(window);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to open window", ex);
        }
    }

    public void OpenNotification(string title, string message, bool copyVisible, bool doDialog, Window? windowForDialog = null)
    {
        _logger.Debug("Creating notif window (Title=\"{title}\", Msg=\"{msg}\", CopyV={copyV})",
            title, message, copyVisible);

        var vmRes = _notificationWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;

        var vm = vmRes.Value;
        vm.WindowTitle = title;
        vm.Notification = message;
        vm.CopyClipboardVisible = copyVisible;

        _sound?.PlayNotificationSound();
        Open(() => new NotificationWindow(), vm, doDialog, windowForDialog, null);
    }

    public void OpenEditDict(string title, string keyName, string valueName, Dictionary<string,string> dict, Window? parentWindow, Action? onClose = null)
    {
        _logger.Debug("Creating dictionary editor (Title=\"{title}\", Key=\"{key}\", Value={value})",
            title, keyName, valueName);

        var vmRes = _editDictWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;
        vmRes.Value.Init(title, keyName, valueName, dict);

        Open(() => new EditDictWindow(), vmRes.Value, true, parentWindow, onClose);
    }

    public void OpenDisplayList(string title, string valueName, string[] values, Window? parentWindow)
    {
        _logger.Debug("Creating list display (Title=\"{title}\", ValueName=\"{key}\")", title, valueName);

        var vmRes = _displayListWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;
        vmRes.Value.Init(values, title, valueName);

        Open(() => new DisplayListWindow(), vmRes.Value, true, parentWindow, null);
    }

    public void OpenEditApiPresets(List<ApiPresetModel> list, Window? parentWindow, Action? onClose = null)
    {
        _logger.Debug("Creating api preset editor");

        var vmRes = _editApiPresetsWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;
        vmRes.Value.Init(list);

        Open(() => new EditApiPresetsWindow(), vmRes.Value, true, parentWindow, onClose);
    }

    public void OpenEditCounters(List<CounterModel> list, Window? parentWindow, Action? onClose = null)
    {
        _logger.Debug("Creating counter editor");

        var vmRes = _editCountersWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;
        vmRes.Value.Init(list);

        Open(() => new EditCountersWindow(), vmRes.Value, true, parentWindow, onClose);
    }

    public void OpenEditFilters(List<FilterModel> list, Window? parentWindow, Action? onClose = null)
    {
        _logger.Debug("Creating filter editor");

        var vmRes = _editFiltersWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;
        vmRes.Value.Init(list);

        Open(() => new EditFiltersWindow(), vmRes.Value, true, parentWindow, onClose);
    }

    public void OpenEditList(List<string> values, string title, string valueName, Window? parentWindow, Action? onClose = null)
    {
        _logger.Debug("Creating list editor with title {title} and valueName {values}", title, valueName);

        var vmRes = _editListWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;
        vmRes.Value.InitExtra(values, title, valueName);

        Open(() => new EditListWindow(), vmRes.Value, true, parentWindow, onClose);
    }

    public void OpenEditOscRelayFilters(List<OscRelayFilterModel> values, Window? parentWindow, Action? onClose = null)
    {
        _logger.Debug("Creating OSC relay filter editor");

        var vmRes = _editOscRelayFiltersWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;
        vmRes.Value.Init(values);

        Open(() => new EditOscRelayFiltersWindow(), vmRes.Value, true, parentWindow, onClose);
    }

    public void OpenEditReplacements(List<ReplacementDataModel> values, Window? parentWindow, Action? onClose = null)
    {
        _logger.Debug("Creating replacement editor");

        var vmRes = _editReplacementsWvmLoader.GetInstance();
        if (!vmRes.IsOk) return;
        vmRes.Value.Init(values);

        Open(() => new EditReplacementsWindow(), vmRes.Value, true, parentWindow, onClose);
    }
}