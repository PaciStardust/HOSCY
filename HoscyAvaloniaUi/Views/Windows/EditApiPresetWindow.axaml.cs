using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class EditApiPresetWindow : Window
{
    public EditApiPresetWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as EditApiPresetWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }

    private void AddEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditApiPresetWindowViewModelBase)?.AddEntry();
        e.Handled = true;
    }

    private void RemoveEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditApiPresetWindowViewModelBase)?.RemoveEntry();
        e.Handled = true;
    }

    private void ModifyEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditApiPresetWindowViewModelBase)?.ModifyEntry();
        e.Handled = true;
    }

    private void EditHeaders(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditApiPresetWindowViewModelBase)?.EditHeaders(this);
        e.Handled = true;
    }

    private void KeyPressed(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        (DataContext as EditApiPresetWindowViewModelBase)?.KeyPressed(e);
        e.Handled = true;
    }
}