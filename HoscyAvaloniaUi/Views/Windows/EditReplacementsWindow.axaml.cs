using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class EditReplacementsWindow : Window
{
    public EditReplacementsWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as EditReplacementsWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }

    private void AddEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditReplacementsWindowViewModelBase)?.AddEntry();
        e.Handled = true;
    }

    private void RemoveEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditReplacementsWindowViewModelBase)?.RemoveEntry();
        e.Handled = true;
    }

    private void ModifyEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditReplacementsWindowViewModelBase)?.ModifyEntry();
        e.Handled = true;
    }

    private void KeyPressed(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        (DataContext as EditReplacementsWindowViewModelBase)?.KeyPressed(e);
        e.Handled = true;
    }
}