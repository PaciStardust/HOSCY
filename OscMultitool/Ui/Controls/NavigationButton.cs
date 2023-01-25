using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hoscy.Ui.Controls
{
    internal class NavigationButton : ListBoxItem
    {
        static NavigationButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationButton), new FrameworkPropertyMetadata(typeof(NavigationButton)));
        }

        internal Uri NavPage
        {
            get { return (Uri)GetValue(NavPageProperty); }
            set { SetValue(NavPageProperty, value); }
        }
        internal static readonly DependencyProperty NavPageProperty =
            DependencyProperty.Register("NavPage", typeof(Uri), typeof(NavigationButton), new PropertyMetadata(null));

        internal string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        internal static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(NavigationButton), new PropertyMetadata("Untitled"));

        internal SolidColorBrush Color
        {
            get { return (SolidColorBrush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
        internal static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(SolidColorBrush), typeof(NavigationButton), new PropertyMetadata(null));
    }
}
