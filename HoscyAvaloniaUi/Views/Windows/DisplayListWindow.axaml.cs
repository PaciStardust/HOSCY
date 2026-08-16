using Avalonia.Controls;
using HoscyAvaloniaUi.ViewModels.Windows;

namespace HoscyAvaloniaUi.Views.Windows;

public partial class DisplayListWindow : Window
{
    public DisplayListWindow()
    {
        InitializeComponent();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (DataContext as DisplayListWindowViewModelBase)?.SelectionChanged();
        e.Handled = true;
    }
}