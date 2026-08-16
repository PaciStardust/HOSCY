using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class ExtrasSubMenu : UserControl
{
    public ExtrasSubMenu()
    {
        InitializeComponent();
    }

    private void AfkSkipClicked(object? sender, RoutedEventArgs args)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.AfkSkipClicked();
        args.Handled = true;
    }

    private void CountersEditClicked(object? sender, RoutedEventArgs args)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.CountersEditClicked();
        args.Handled = true;
    }

    private void MediaBackendChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.MediaBackendChanged();
        e.Handled = true;
    }

    private void MediaBackendReloadClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.MediaBackendReloadClicked();
        e.Handled = true;
    }

    private void MediaBackendEndpointsClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.MediaBackendEndpointsClicked();
        e.Handled = true;
    }

    private void MediaFiltersClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.MediaFiltersClicked();
        e.Handled = true;
    }

    private void MediaMprisEndpointsPreferredClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.MediaMprisEndpointsPreferredClicked();
        e.Handled = true;
    }

    private void MediaMprisEndpointsIgnoredClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.MediaMprisEndpointsIgnoredClicked();
        e.Handled = true;
    }
}