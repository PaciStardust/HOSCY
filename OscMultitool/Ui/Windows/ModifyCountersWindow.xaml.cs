using System;
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
    public partial class ModifyCountersWindow : Window
    {
        private readonly List<Config.CounterModel> _list;

        public ModifyCountersWindow(string title, List<Config.CounterModel> list)
        {
            InitializeComponent();
            
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

            textName.Text = _list[listBox.SelectedIndex].Name;
            textParameter.Text = _list[listBox.SelectedIndex].Parameter;
            textCount.Text = _list[listBox.SelectedIndex].Count.ToString();
        }

        private Config.CounterModel GetNewModel()
        {
            var model = new Config.CounterModel();

            if (!string.IsNullOrWhiteSpace(textName.Text))
                model.Name = textName.Text;

            if (!string.IsNullOrWhiteSpace(textParameter.Text))
                model.Parameter = textParameter.Text;

            try
            {
                model.Count = Convert.ToUInt32(textCount.Text);
            }
            catch { }

            return model;
        }
        #endregion
    }
}
