using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for ModifyListWindow.xaml
    /// </summary>
    internal partial class ModifyListWindow : Window
    {
        private readonly List<string> _list;
        private readonly string _default;

        public ModifyListWindow(string title, string valueName, List<string> list, string defaultString = "New Value")
        {
            InitializeComponent();

            Closed += (s, a) => Config.SaveConfig();

            labelValue.Text = valueName;
            textValue.Tag = valueName + "...";

            _list = list;
            Title = title;
            _default = defaultString;
            Refresh(-1);
        }
        
        private void Refresh(int index)
            => listBox.Refresh(_list, index);

        #region Modification
        private void AddEntry()
        {
            string value = GetTextValue(textValue.Text);
            if (string.IsNullOrWhiteSpace(value))
                return;

            _list.Add(value);
            Refresh(_list.Count - 1);
        }

        private void TextBox_KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddEntry();
        }

        private void Button_AddEntry(object sender, RoutedEventArgs e)
            => AddEntry();

        private void Button_RemoveEntry(object sender, RoutedEventArgs e)
        {
            int index = listBox.SelectedIndex;
            if (_list.TryRemoveAt(index))
                Refresh(index - 1);
        }

        private void Button_ModifyEntry(object sender, RoutedEventArgs e)
        {
            var text = GetTextValue(textValue.Text);
            int index = listBox.SelectedIndex;

            if (string.IsNullOrWhiteSpace(text) || !_list.TryModifyAt(text, index))
                return;

            Refresh(index);
        }
        #endregion

        #region Other
        private string GetTextValue(string text)
        {
            text = text.Trim();
            return string.IsNullOrWhiteSpace(text) ? _default : text;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!listBox.IsInBounds(_list))
                return;

            textValue.Text = _list[listBox.SelectedIndex];
        }
        #endregion
    }
}
