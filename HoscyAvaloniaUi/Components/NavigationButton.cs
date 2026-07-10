using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HoscyAvaloniaUi.Components;

public class NavigationButton : ListBoxItem
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<NavigationButton, string>(nameof(Title), string.Empty);
    public string Title
    {
        get { return GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    public static readonly StyledProperty<SolidColorBrush> ColorProperty =
        AvaloniaProperty.Register<NavigationButton, SolidColorBrush>(nameof(Color), new SolidColorBrush(Colors.Red));
    public SolidColorBrush Color
    {
        get { return GetValue(ColorProperty); }
        set { SetValue(ColorProperty, value); }
    }

    public static readonly StyledProperty<bool> SelectedProperty =
        AvaloniaProperty.Register<NavigationButton, bool>(nameof(Selected), false);
    public bool Selected
    {
        get { return GetValue(SelectedProperty); }
        set { SetValue(SelectedProperty, value); }
    }
}