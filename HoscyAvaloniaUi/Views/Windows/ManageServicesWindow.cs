using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class ManageServicesWindow : Window
{
    public ManageServicesWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as ManageServicesWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }

    private void RefreshClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as ManageServicesWindowViewModelBase)?.RefreshClicked();
        e.Handled = true;
    }

    private void RestartClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as ManageServicesWindowViewModelBase)?.RestartClicked();
        e.Handled = true;
    }

    private void StartStopClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as ManageServicesWindowViewModelBase)?.StartStopClicked();
        e.Handled = true;
    }

    private void CopyClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as ManageServicesWindowViewModelBase)?.CopyClicked(Clipboard);
        e.Handled = true;
    }
}