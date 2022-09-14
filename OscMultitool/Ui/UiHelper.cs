using System.Collections.Generic;
using System.Windows.Controls;

namespace Hoscy.Ui
{
    public static class UiHelper
    {
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
