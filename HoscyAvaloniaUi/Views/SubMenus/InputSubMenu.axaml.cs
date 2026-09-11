using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class InputSubMenu : UserControl
{
    public InputSubMenu()
    {
        InitializeComponent();
    }

    private void PresetSelected(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as InputSubMenuViewModelBase)?.PresetSelected();
        e.Handled = true;
    }

    private void PresetsClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as InputSubMenuViewModelBase)?.PresetsClicked();
        e.Handled = true;
    }
    private void HistoryClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as InputSubMenuViewModelBase)?.HistoryClicked();
        e.Handled = true;
    }
    private void SendClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as InputSubMenuViewModelBase)?.SendClicked();
        e.Handled = true;
    }
    private void ClearClicked(object? sender, RoutedEventArgs e)
    {
        (DataContext as InputSubMenuViewModelBase)?.ClearClicked();
        e.Handled = true;
    }
}