using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.SubMenus;

namespace HoscyAvaloniaUi.Views.SubMenus;

public partial class InfoSubMenu : UserControl
{
    public InfoSubMenu()
    {
        InitializeComponent();
    }

    private void ButtonClearClicked(object? sender, RoutedEventArgs args)
    {
        (DataContext as InfoSubMenuViewModelBase)?.ButtonClearClicked();
        args.Handled = true;
    }
    private void ButtonStartStopClicked(object? sender, RoutedEventArgs args)
    {
        (DataContext as InfoSubMenuViewModelBase)?.ButtonStartStopClicked();
        args.Handled = true;
    }
    private void ButtonToggleListeningClicked(object? sender, RoutedEventArgs args)
    {
        (DataContext as InfoSubMenuViewModelBase)?.ButtonToggleListeningClicked();
        args.Handled = true;
    }
}