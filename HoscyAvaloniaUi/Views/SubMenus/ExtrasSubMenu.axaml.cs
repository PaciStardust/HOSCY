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

    private void EditCountersClicked(object? sender, RoutedEventArgs args)
    {
        (DataContext as ExtrasSubMenuViewModelBase)?.EditCountersClicked();
        args.Handled = true;
    }
}