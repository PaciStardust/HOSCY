using Hoscy.Models;
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
    internal partial class ModifyReplacementDataWindow : Window
    {
        private readonly List<ReplacementDataModel> _list;

        public ModifyReplacementDataWindow(string title, List<ReplacementDataModel> list)
        {
            InitializeComponent();

            Closed += (s, a) => Config.SaveConfig();

            _list = list;
            Title = title;
            Refresh(-1);
        }

        private void Refresh(int index)
            => listBox.Load(_list.Select(x => x.ToString()), index);

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
            regexCheckBox.IsChecked = _list[listBox.SelectedIndex].UseRegex;
            ignoreCaseCheckBox.IsChecked = _list[listBox.SelectedIndex].IgnoreCase;
        }

        private ReplacementDataModel GetNewModel()
        {
            var model = new ReplacementDataModel()
            {
                Enabled = enabledCheckBox.IsChecked ?? false,
                UseRegex = regexCheckBox.IsChecked ?? false,
                IgnoreCase = ignoreCaseCheckBox.IsChecked ?? false
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
