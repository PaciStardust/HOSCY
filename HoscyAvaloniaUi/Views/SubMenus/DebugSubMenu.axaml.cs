using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class DebugSubMenu : UserControl
{
    public DebugSubMenu()
    {
        InitializeComponent();
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
    private void UtilReloadDevices(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as DebugSubMenuViewModelBase)?.UtilReloadDevices();
        e.Handled = true;
    }
}