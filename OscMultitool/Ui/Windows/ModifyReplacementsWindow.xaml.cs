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
    public partial class ModifyReplacementsWindow : Window
    {
        private readonly List<Config.ReplacementModel> _list;

        public ModifyReplacementsWindow(string title, List<Config.ReplacementModel> list)
        {
            InitializeComponent();
            
            _list = list;
            Title = title;
            Refresh();
        }

        private void Refresh()
            => UiHelper.ListBoxRefresh(listBox, _list.Select(x => x.ToString()));

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

            textValue.Text = _list[listBox.SelectedIndex].Text;
            replacementValue.Text = _list[listBox.SelectedIndex].Replacement;
            enabledCheckBox.IsChecked = _list[listBox.SelectedIndex].Enabled;
        }

        private Config.ReplacementModel GetNewModel()
        {
            return new
            (
                textValue.Text,
                replacementValue.Text,
                enabledCheckBox.IsChecked ?? false
            );
        }
    }
}
