using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class DebugSubMenu : UserControl
{
    public DebugSubMenu()
    {
        InitializeComponent();
    }

    private void InfoSpeakerChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.InfoSpeakerChanged();
        e.Handled = true;
    }
    private void InfoSpeakerRefreshClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.InfoSpeakerRefreshClicked();
        e.Handled = true;
    }
    private void InfoSpeakerApplyClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.InfoSpeakerApplyClicked();
        e.Handled = true;
    }

    private void InfoSpeakerVolumeChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.InfoSpeakerVolumeChanged();
        e.Handled = true;
    }

    private void LogLevelChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.LogLevelChanged();
        e.Handled = true;
    }

    private void LogFiltersClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.LogFiltersClicked();
        e.Handled = true;
    }

    private void UtilOpenGit(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.UtilOpenGit();
        e.Handled = true;
    }
    private void UtilOpenConfig(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.UtilOpenConfig();
        e.Handled = true;
    }
    private void UtilSaveConfig(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.UtilSaveConfig();
        e.Handled = true;
    }
    private void UtilManageServices(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.UtilManageServices();
        e.Handled = true;
    }
}