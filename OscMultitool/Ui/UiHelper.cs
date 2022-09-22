using System;
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
        public static void Load(this ComboBox box, IEnumerable<string> source, int index)
        {
            box.ItemsSource = source;
            box.SelectedIndex = index;
        }

        /// <summary>
        /// Refreshes the data
        /// </summary>
        public static void Refresh(this ListBox box, IEnumerable<string> source, int index)
        {
            box.ItemsSource = source;
            box.Items.Refresh();
            box.SelectedIndex = index;
        }

        /// <summary>
        /// Checks if a value is in bounds
        /// </summary>
        public static bool IsInBounds<T>(this ListBox box, ICollection<T> list)
        {
            int highVal = list.Count - 1;
            if (box.SelectedIndex > highVal)
                box.SelectedIndex = highVal;

            return box.SelectedIndex > -1;
        }

        /// <summary>
        /// Tries to remove object at index
        /// </summary>
        public static bool TryRemoveAt<T>(this List<T> list, int index)
        {
            if (index < 0 || index >= list.Count)
                return false;

            list.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Tries to modify object at index
        /// </summary>
        public static bool TryModifyAt<T>(this List<T> list, T item, int index)
        {
            if (index < 0 || index >= list.Count)
                return false;

            list[index] = item;
            return true;
        }
    }
}
