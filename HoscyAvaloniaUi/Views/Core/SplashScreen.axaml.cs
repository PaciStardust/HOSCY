using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using HoscyAvaloniaUi.ViewModels.Core;

namespace HoscyAvaloniaUi.Views.Core;

public partial class SplashScreen : UserControl
{
    public IClipboard? Clipboard { get; set; } = null;

    public SplashScreen()
    {
        InitializeComponent();
    }

    private void CopyErrorClicked(object? sender, RoutedEventArgs args)
    {
        (DataContext as SplashScreenViewModel)?.CopyErrorClicked(Clipboard);
        args.Handled = true;
    }
}