using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using HoscyAvaloniaUi.ViewModels.Windows;
using HoscyAvaloniaUi.Views.Windows;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using Serilog;

namespace HoscyAvaloniaUi.Services;

[LoadIntoDiContainer(typeof(NotificationWindowFactory), Lifetime.Singleton)]
public class NotificationWindowFactory
(
    ILogger logger,
    IContainerBulkLoader<IApplicationSound> soundLoader,
    IContainerBulkLoader<NotificationWindowViewModelBase> vmLoader,
    UiHelperService windowWrapper
) 
    : IService
{
    private readonly ILogger _logger = logger.ForContext<NotificationWindowFactory>();
    private readonly IApplicationSound? _sound = soundLoader.GetInstances().Value?.FirstOrDefault();
    private readonly IContainerBulkLoader<NotificationWindowViewModelBase> _vmLoader = vmLoader;
    private readonly UiHelperService _windowWrapper = windowWrapper;

    public void CreateAndOpen(string title, string message, bool copyVisible, bool doDialog, Window? windowForDialog = null)
    {
        _logger.Debug("Creating notif window (Title=\"{title}\", Msg=\"{msg}\", CopyV={copyV})",
            title, message, copyVisible);
        
        var vmRes = _vmLoader.GetInstance(typeof(NotificationWindowViewModelBase));
        if (!vmRes.IsOk) return;

        var vm = vmRes.Value;
        vm.WindowTitle = title;
        vm.Notification = message;
        vm.CopyClipboardVisible = copyVisible;

        _sound?.PlayNotificationSound();
        Dispatcher.UIThread.Invoke(() =>
        {
            var window = new NotificationWindow() { DataContext = vm };
            if (!doDialog)
            {
                window.Show();
            }
            else
            {
                if (windowForDialog is not null)
                {
                    window.ShowDialog(windowForDialog);
                }
                else
                {
                    _windowWrapper.ShowDialog(window);
                }
            }
        });
    }
}