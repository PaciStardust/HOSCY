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
    internal partial class ModifyApiPresetsWindow : Window
    {
        private readonly List<ApiPresetModel> _list;

        public ModifyApiPresetsWindow(List<ApiPresetModel> list)
        {
            InitializeComponent();

            Closed += (s, a) => Config.SaveConfig();

            _list = list;
            Refresh(-1);
        }

        private void Refresh(int index)
            => listBox.Load( _list.Select(x => x.Name), index);

        #region Modification
        private void AddEntry()
        {
            _list.Add(GetNewModel());
            Refresh(listBox.Items.Count - 1);
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

            var selected = _list[listBox.SelectedIndex];
            textName.Text = selected.Name;
            textUrl.Text = selected.TargetUrl;
            textResult.Text = selected.ResultField;
            textTimeout.Text = selected.ConnectionTimeout.ToString();
            textJson.Text = selected.SentData;
            textContentType.Text = selected.ContentType;
            textAuthHeader.Text = selected.Authorization;
        }

        private ApiPresetModel GetNewModel()
        {
            var model = new ApiPresetModel();

            if (!string.IsNullOrWhiteSpace(textName.Text))
                model.Name = textName.Text;

            if (!string.IsNullOrWhiteSpace(textUrl.Text))
                model.TargetUrl = textUrl.Text;

            if (!string.IsNullOrWhiteSpace(textResult.Text))
                model.ResultField = textResult.Text;

            if (int.TryParse(textTimeout.Text, out int timeout))
                model.ConnectionTimeout = timeout;

            if (!string.IsNullOrWhiteSpace(textJson.Text))
                model.SentData = textJson.Text;

            if (!string.IsNullOrWhiteSpace(textContentType.Text))
                model.ContentType = textContentType.Text;

            if (listBox.SelectedIndex != -1)
                model.HeaderValues = _list[listBox.SelectedIndex].HeaderValues.ToDictionary(x => x.Key, x => x.Value);

            if (!string.IsNullOrWhiteSpace(textAuthHeader.Text))
                model.Authorization = textAuthHeader.Text;

            return model;
        }

        private void Button_EditHeaders(object sender, RoutedEventArgs e)
        {
            if (!listBox.IsInBounds(_list))
                return;

            var index = listBox.SelectedIndex;
            var selected = _list[index];
            UiHelper.OpenDictionaryEditor($"Modify Headers: {selected.Name}", "Name", "Value", selected.HeaderValues);
            Refresh(index);
        }
        #endregion
    }
}
