using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class EditDictWindow : Window
{
    public EditDictWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as EditDictWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }

    private void AddNewOrModify(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditDictWindowViewModelBase)?.AddOrModifyEntry();
        e.Handled = true;
    }

    private void RemoveEntry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as EditDictWindowViewModelBase)?.RemoveEntry();
        e.Handled = true;
    }

    private void KeyReleased(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        (DataContext as EditDictWindowViewModelBase)?.KeyPressed(e);
        e.Handled = true;
    }
}