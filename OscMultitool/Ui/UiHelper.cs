using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hoscy.Ui
{
    public static class UiHelper
    {
        public static readonly SolidColorBrush ColorGrayDark = new(Color.FromRgb(37, 37, 37));
        public static readonly SolidColorBrush ColorGray = new(Color.FromRgb(68, 68, 68));
        public static readonly SolidColorBrush ColorGrayLight = new(Color.FromRgb(96, 96, 96));
        public static readonly SolidColorBrush ColorWhite = new(Color.FromRgb(255, 255, 255));
        public static readonly SolidColorBrush ColorBlack = new(Color.FromRgb(0, 0, 0));
        public static readonly SolidColorBrush ColorAccent = new(Color.FromRgb(60, 255, 60));
        public static readonly SolidColorBrush ColorError = new(Color.FromRgb(60, 60, 255));

        public static readonly int FontSRegular = 18;

        /// <summary>
        /// Loads data into a combo box
        /// </summary>
        /// <param name="box">Combo box to load data for</param>
        /// <param name="source">Source of data</param>
        /// <param name="index">Preselected Index</param>
        public static void LoadComboBox(ComboBox box, IEnumerable<string> source, int index)
        {
            box.ItemsSource = source;
            box.SelectedIndex = index;
        }

        /// <summary>
        /// Refreshes data in a list box
        /// </summary>
        /// <param name="box">list box to refresh</param>
        /// <param name="source">Source of data</param>
        /// <param name="index">Preselected Index</param>
        public static void RefreshListBox(ListBox box, IEnumerable<string> source)
        {
            box.ItemsSource = source;
            box.Items.Refresh();
        }

        /// <summary>
        /// Set a panel as active or inactive
        /// </summary>
        /// <param name="ctrl">Panel to set acitivity for</param>
        /// <param name="status">Status</param>
        public static void SetEnabled(Panel ctrl, bool status)
        {
            if (ctrl == null)
                return;

            ctrl.IsEnabled = status;
            ctrl.Opacity = status ? 1 : 0.5f;
        }
    }
}
