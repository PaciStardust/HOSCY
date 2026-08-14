using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class EditFiltersWindow : Window
{
    public EditFiltersWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as EditFiltersWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }

    private void AddEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditFiltersWindowViewModelBase)?.AddEntry();
        e.Handled = true;
    }

    private void RemoveEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditFiltersWindowViewModelBase)?.RemoveEntry();
        e.Handled = true;
    }

    private void ModifyEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditFiltersWindowViewModelBase)?.ModifyEntry();
        e.Handled = true;
    }

    private void KeyPressed(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        (DataContext as EditFiltersWindowViewModelBase)?.KeyPressed(e);
        e.Handled = true;
    }
}