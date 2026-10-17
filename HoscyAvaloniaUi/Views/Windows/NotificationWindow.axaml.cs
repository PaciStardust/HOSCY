using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class NotificationWindow : Window
{
    public NotificationWindow()
    {
        InitializeComponent();
    }

    private void OnRepoClick(object? _, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as NotificationWindowViewModelBase)?.OnRepoClick();
        e.Handled = true;
    }

    private void OnClipboardClick(object? _, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = DataContext as NotificationWindowViewModelBase;
        vm?.OnClipboardClick(Clipboard);
        e.Handled = true;
    }
}