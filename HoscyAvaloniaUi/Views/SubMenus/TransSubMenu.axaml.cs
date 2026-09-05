using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class TransSubMenu : UserControl
{
    public TransSubMenu()
    {
        InitializeComponent();
    }

    private void OptionsSelectedModuleChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as TransSubMenuViewModelBase)?.OptionsSelectedModuleChanged();
        e.Handled = true;
    }
    private void OptionsSelectedModuleStartStopClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as TransSubMenuViewModelBase)?.OptionsSelectedModuleStartStopClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleRefreshClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as TransSubMenuViewModelBase)?.OptionsSelectedModuleRefreshClicked();
        e.Handled = true;
    }
    private void OptionsSelectedModuleRestartClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as TransSubMenuViewModelBase)?.OptionsSelectedModuleRestartClicked();
        e.Handled = true;
    }
}