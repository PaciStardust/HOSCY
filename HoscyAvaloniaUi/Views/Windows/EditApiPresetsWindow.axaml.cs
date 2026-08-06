using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class EditApiPresetsWindow : Window
{
    public EditApiPresetsWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as EditApiPresetsWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }

    private void AddEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditApiPresetsWindowViewModelBase)?.AddEntry();
        e.Handled = true;
    }

    private void RemoveEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditApiPresetsWindowViewModelBase)?.RemoveEntry();
        e.Handled = true;
    }

    private void ModifyEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditApiPresetsWindowViewModelBase)?.ModifyEntry();
        e.Handled = true;
    }

    private void EditHeaders(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditApiPresetsWindowViewModelBase)?.EditHeaders(this);
        e.Handled = true;
    }

    private void KeyPressed(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        (DataContext as EditApiPresetsWindowViewModelBase)?.KeyPressed(e);
        e.Handled = true;
    }
}