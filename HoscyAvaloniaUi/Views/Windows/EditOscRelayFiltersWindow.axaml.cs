using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class EditOscRelayFiltersWindow : Window
{
    public EditOscRelayFiltersWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as EditOscRelayFiltersWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }

    private void AddEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditOscRelayFiltersWindowViewModelBase)?.AddEntry();
        e.Handled = true;
    }

    private void RemoveEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditOscRelayFiltersWindowViewModelBase)?.RemoveEntry();
        e.Handled = true;
    }

    private void ModifyEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditOscRelayFiltersWindowViewModelBase)?.ModifyEntry();
        e.Handled = true;
    }

    private void FiltersClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditOscRelayFiltersWindowViewModelBase)?.FiltersClicked(this);
        e.Handled = true;
    }

    private void KeyPressed(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        (DataContext as EditOscRelayFiltersWindowViewModelBase)?.KeyPressed(e);
        e.Handled = true;
    }
}