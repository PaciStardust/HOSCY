using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OscMultitool.Ui.Windows
{
    /// <summary>
    /// Interaction logic for ModifyListWindow.xaml
    /// </summary>
    public partial class ModifyListWindow : Window
    {
        private readonly List<string> _list;
        private readonly string _default;

        public ModifyListWindow(string title, List<string> list, string defaultString = "")
        {
            InitializeComponent();
            
            _list = list;
            Title = title;
            _default = defaultString;
            listBox.ItemsSource = _list;
        }

        private void AddEntry()
        {
            string value = GetTextValue(textValue.Text);
            if (string.IsNullOrWhiteSpace(value))
                return;
            
            _list.Add(value);
            listBox.Items.Refresh();
            listBox.SelectedIndex = _list.Count - 1;
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
            if (_list.Count == 0 || listBox.SelectedIndex == -1)
                return;

            int index = listBox.SelectedIndex;
            _list.RemoveAt(index);
            listBox.Items.Refresh();
            listBox.SelectedIndex = index - 1;
        }

        private void Button_ModifyEntry(object sender, RoutedEventArgs e)
        {
            if (_list.Count == 0 || listBox.SelectedIndex == -1)
                return;

            _list[listBox.SelectedIndex] = textValue.Text;
            listBox.Items.Refresh();
        }

        private string GetTextValue(string text)
        {
            text = text.Trim();
            return string.IsNullOrWhiteSpace(text) ? _default : text;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox.SelectedIndex > _list.Count - 1)
                listBox.SelectedIndex = _list.Count - 1;

            if (listBox.SelectedIndex < 0)
                return;

            textValue.Text = _list[listBox.SelectedIndex];
        }
    }
}
