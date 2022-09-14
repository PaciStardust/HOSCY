using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hoscy.Ui.Controls
{
    public class NavigationButton : ListBoxItem
    {
        static NavigationButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationButton), new FrameworkPropertyMetadata(typeof(NavigationButton)));
        }

        public Uri NavPage
        {
            get { return (Uri)GetValue(NavPageProperty); }
            set { SetValue(NavPageProperty, value); }
        }
        public static readonly DependencyProperty NavPageProperty =
            DependencyProperty.Register("NavPage", typeof(Uri), typeof(NavigationButton), new PropertyMetadata(null));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(NavigationButton), new PropertyMetadata("Untitled"));

        public SolidColorBrush Color
        {
            get { return (SolidColorBrush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(SolidColorBrush), typeof(NavigationButton), new PropertyMetadata(null));
    }
}
