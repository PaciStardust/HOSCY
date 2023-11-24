using Hoscy.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for ModifyListWindow.xaml
    /// </summary>
    internal partial class ModifyCountersWindow : Window
    {
        private readonly List<CounterModel> _list;

        public ModifyCountersWindow(List<CounterModel> list)
        {
            InitializeComponent();

            Closed += (s, a) => Config.SaveConfig();

            _list = list;
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

            textName.Text = _list[listBox.SelectedIndex].Name;
            textParameter.Text = _list[listBox.SelectedIndex].Parameter;
            textCount.Text = _list[listBox.SelectedIndex].Count.ToString();
            enabledCheckBox.IsChecked = _list[listBox.SelectedIndex].Enabled;
            textCooldown.Text = _list[listBox.SelectedIndex].Cooldown.ToString();
        }

        private CounterModel GetNewModel()
        {
            var model = new CounterModel()
            {
                Enabled = enabledCheckBox.IsChecked ?? false
            };

            if (!string.IsNullOrWhiteSpace(textName.Text))
                model.Name = textName.Text;

            if (!string.IsNullOrWhiteSpace(textParameter.Text))
                model.Parameter = textParameter.Text;

            try
            {
                model.Count = Convert.ToUInt32(textCount.Text);
                model.Cooldown = float.Parse(textCooldown.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch { }

            return model;
        }
        #endregion
    }
}
