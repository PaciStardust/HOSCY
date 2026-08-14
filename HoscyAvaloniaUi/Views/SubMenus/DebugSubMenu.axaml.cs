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
}