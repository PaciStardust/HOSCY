using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class OscSubMenu : UserControl
{
    public OscSubMenu()
    {
        InitializeComponent();
    }

    private void RelayFiltersClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as OscSubMenuViewModelBase)?.RelayFiltersClicked();
        e.Handled = true;
    }

    private void QueryServicesClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as OscSubMenuViewModelBase)?.QueryServicesClicked();
        e.Handled = true;
    }

    private void ListeningPortChanged(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        (DataContext as OscSubMenuViewModelBase)?.ListeningPortChanged();
        e.Handled = true;
    }

    private void ReloadListenerClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as OscSubMenuViewModelBase)?.ReloadListenerClicked();
        e.Handled = true;
    }
}