using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class RecogSubMenu : UserControl
{
    public RecogSubMenu()
    {
        InitializeComponent();
    }

    private void OptionsSelectedModuleChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleChanged();
        e.Handled = true;
    }
    private void OptionsSelectedModuleStartStopClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleStartStopClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleRefreshClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleRefreshClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleRestartClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleRestartClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleToggleMuteClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsSelectedModuleToggleMuteClicked();
        e.Handled = true;
    }

    private void OptionsOutputNoiseFilterClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsOutputNoiseFilterClicked();
        e.Handled = true;
    }

    private void OptionsMicrophoneChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsMicrophoneChanged();
        e.Handled = true;
    }

    private void OptionsMicrophoneRefreshClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as RecogSubMenuViewModelBase)?.OptionsMicrophoneRefreshClicked();
        e.Handled = true;
    }
}