using Avalonia.Controls;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.Core;

namespace HoscyAvaloniaUi.Views.Core;

public partial class CoreMenu : UserControl
{
    public CoreMenu()
    {
        InitializeComponent();
        MenuList.Loaded += (_, _) => MenuList.SelectedIndex = 0;
    }

    public void OnMenuSelected(object? _, SelectionChangedEventArgs args)
    {
        (DataContext as CoreMenuViewModelBase)?.OnMenuSelected(MenuList);
        args.Handled = true;
    }

    private void HideBanner(object? sender, RoutedEventArgs args)
    {
        (DataContext as CoreMenuViewModelBase)?.HideBanner();
        args.Handled = true;
    }
}