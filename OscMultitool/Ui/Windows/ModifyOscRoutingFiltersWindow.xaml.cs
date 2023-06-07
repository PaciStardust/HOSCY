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
    internal partial class ModifyOscRoutingFiltersWindow : Window
    {
        private readonly List<OscRoutingFilterModel> _list;

        public ModifyOscRoutingFiltersWindow(string title, List<OscRoutingFilterModel> list)
        {
            InitializeComponent();

            Closed += (s, a) => Config.SaveConfig();

            _list = list;
            Title = title;
            Refresh(-1);
        }

        private void Refresh(int index)
            => listBox.Load( _list.Select(x => $"{x} ({x.Filters.Count} Filters)"), index);

        #region Modification
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
            textPort.Text = _list[listBox.SelectedIndex].Port.ToString();
            textIp.Text = _list[listBox.SelectedIndex].Ip;
        }

        private OscRoutingFilterModel GetNewModel()
        {
            var model = new OscRoutingFilterModel();

            if (!string.IsNullOrWhiteSpace(textName.Text))
                model.Name = textName.Text;

            if (!string.IsNullOrWhiteSpace(textIp.Text))
                model.Ip = textIp.Text;

            if (int.TryParse(textPort.Text, out int port))
                model.Port = port;

            if (listBox.SelectedIndex != -1)
                model.Filters = _list[listBox.SelectedIndex].Filters.ToList();

            return model;
        }

        private void Button_EditFilters(object sender, RoutedEventArgs e)
        {
            if (!listBox.IsInBounds(_list))
                return;

            var index = listBox.SelectedIndex;
            var selected = _list[index];
            UiHelper.OpenListEditor($"Edit Filter: {selected}", "OSC Filter", selected.Filters, "/");
            Refresh(index);
        }
        #endregion
    }
}
