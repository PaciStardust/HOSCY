using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hoscy.Ui
{
    public static class UiHelper
    {
        //Back
        public static readonly SolidColorBrush ColorBackDark = new(Color.FromRgb(37, 37, 37));
        public static readonly SolidColorBrush ColorBack = new(Color.FromRgb(52, 52, 52));
        public static readonly SolidColorBrush ColorBackLight = new(Color.FromRgb(77, 77, 77));

        //Front
        public static readonly SolidColorBrush ColorFront = new(Color.FromRgb(255, 255, 255));
        public static readonly SolidColorBrush ColorFrontDark = new(Color.FromRgb(200, 200, 200));

        //Extra
        public static readonly SolidColorBrush ColorValid = new(Color.FromRgb(202, 255, 191));
        public static readonly SolidColorBrush ColorInvalid = new(Color.FromRgb(255, 173, 173));

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
        public static void ListBoxRefresh(ListBox box, IEnumerable<string> source)
        {
            box.ItemsSource = source;
            box.Items.Refresh();
        }
    }
}
