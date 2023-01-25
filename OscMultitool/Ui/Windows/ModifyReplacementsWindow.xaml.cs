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
    internal partial class ModifyReplacementsWindow : Window
    {
        private readonly List<Config.ReplacementModel> _list;

        public ModifyReplacementsWindow(string title, List<Config.ReplacementModel> list)
        {
            InitializeComponent();

            Closed += (s, a) => Config.SaveConfig();

            _list = list;
            Title = title;
            Refresh(-1);
        }

        private void Refresh(int index)
            => listBox.Refresh(_list.Select(x => x.ToString()), index);

        #region Modifification
        private void AddEntry()
        {
            _list.Add(GetNewModel());
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
            int index = listBox.SelectedIndex;

            if (_list.TryModifyAt(GetNewModel(), index))
                Refresh(index);
        }
        #endregion

        #region Other
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!listBox.IsInBounds(_list))
                return;

            textValue.Text = _list[listBox.SelectedIndex].Text;
            replacementValue.Text = _list[listBox.SelectedIndex].Replacement;
            enabledCheckBox.IsChecked = _list[listBox.SelectedIndex].Enabled;
        }

        private Config.ReplacementModel GetNewModel()
        {
            var model = new Config.ReplacementModel()
            {
                Enabled = enabledCheckBox.IsChecked ?? false
            };

            if (!string.IsNullOrWhiteSpace(textValue.Text))
                model.Text = textValue.Text;

            if (!string.IsNullOrWhiteSpace(replacementValue.Text))
                model.Replacement = replacementValue.Text;

            return model;
        }
        #endregion
    }
}
