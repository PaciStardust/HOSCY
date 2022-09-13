using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OscMultitool.Ui.Windows
{
    /// <summary>
    /// Interaction logic for ModifyListWindow.xaml
    /// </summary>
    public partial class ModifyOscRoutingFiltersWindow : Window
    {
        private readonly List<Config.ConfigOscRoutingFilterModel> _list;

        public ModifyOscRoutingFiltersWindow(string title, List<Config.ConfigOscRoutingFilterModel> list)
        {
            InitializeComponent();
            
            _list = list;
            Title = title;
            Refresh();
        }

        private void Refresh()
            => UiHelper.RefreshListBox(listBox, _list.Select(x => $"{x} ({x.Filters.Count} Filters)"));

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

            textName.Text = _list[listBox.SelectedIndex].Name;
            textPort.Text = _list[listBox.SelectedIndex].Port.ToString();
            textIp.Text = _list[listBox.SelectedIndex].Ip;
        }

        private Config.ConfigOscRoutingFilterModel GetNewModel()
        {
            var model = new Config.ConfigOscRoutingFilterModel();

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
            if (_list.Count == 0 || listBox.SelectedIndex == -1)
                return;

            var selected = _list[listBox.SelectedIndex];
            var window = new ModifyListWindow($"Filter Editor: {selected}", selected.Filters, "/");
            window.ShowDialog();
            Refresh();
        }
    }
}
