using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class EditAzureVoicesWindow : Window
{
    public EditAzureVoicesWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as EditAzureVoicesWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }

    private void AddEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditAzureVoicesWindowViewModelBase)?.AddEntry();
        e.Handled = true;
    }

    private void RemoveEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditAzureVoicesWindowViewModelBase)?.RemoveEntry();
        e.Handled = true;
    }

    private void ModifyEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditAzureVoicesWindowViewModelBase)?.ModifyEntry();
        e.Handled = true;
    }

    private void KeyPressed(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        (DataContext as EditAzureVoicesWindowViewModelBase)?.KeyPressed(e);
        e.Handled = true;
    }
}