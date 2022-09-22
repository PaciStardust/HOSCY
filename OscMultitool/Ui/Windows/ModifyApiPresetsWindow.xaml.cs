using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for ModifyListWindow.xaml
    /// </summary>
    public partial class ModifyApiPresetsWindow : Window
    {
        private readonly List<Config.ApiPresetModel> _list;

        public ModifyApiPresetsWindow(string title, List<Config.ApiPresetModel> list)
        {
            InitializeComponent();
            
            _list = list;
            Title = title;
            Refresh();
        }

        private void Refresh()
            => UiHelper.ListBoxRefresh(listBox, _list.Select(x => x.Name));

        private void AddEntry()
        {
            _list.Add(GetNewModel());
            Refresh();
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
            Refresh();
            listBox.SelectedIndex = index - 1;
        }

        private void Button_ModifyEntry(object sender, RoutedEventArgs e)
        {
            if (_list.Count == 0 || listBox.SelectedIndex == -1)
                return;

            _list[listBox.SelectedIndex] = GetNewModel();
            Refresh();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox.SelectedIndex > _list.Count - 1)
                listBox.SelectedIndex = _list.Count - 1;

            if (listBox.SelectedIndex < 0)
                return;

            var selected = _list[listBox.SelectedIndex];
            textName.Text = selected.Name;
            textUrl.Text = selected.PostUrl;
            textResult.Text = selected.ResultField;
            textTimeout.Text = selected.ConnectionTimeout.ToString();
            textJson.Text = selected.JsonData;
            textContentType.Text = selected.ContentType;
        }

        private Config.ApiPresetModel GetNewModel()
        {
            var model = new Config.ApiPresetModel();

            if (!string.IsNullOrWhiteSpace(textName.Text))
                model.Name = textName.Text;

            if (!string.IsNullOrWhiteSpace(textUrl.Text))
                model.PostUrl = textUrl.Text;

            if (!string.IsNullOrWhiteSpace(textResult.Text))
                model.ResultField = textResult.Text;

            if (int.TryParse(textTimeout.Text, out int timeout))
                model.ConnectionTimeout = timeout;

            if (!string.IsNullOrWhiteSpace(textJson.Text))
                model.JsonData = textJson.Text;

            if (!string.IsNullOrWhiteSpace(textContentType.Text))
                model.ContentType = textContentType.Text;

            if (listBox.SelectedIndex != -1)
                model.HeaderValues = _list[listBox.SelectedIndex].HeaderValues.ToDictionary(x => x.Key, x => x.Value);

            return model;
        }

        private void Button_EditHeaders(object sender, RoutedEventArgs e)
        {
            if (_list.Count == 0 || listBox.SelectedIndex == -1)
                return;

            var selected = _list[listBox.SelectedIndex];
            var window = new ModifyDictionaryWindow($"Header Editor: {selected.Name}", "Name", "Value", selected.HeaderValues);
            window.ShowDialog();
            Refresh();
        }
    }
}
