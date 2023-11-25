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
    internal partial class ModifyAzureTtsVoicesWindow : Window
    {
        private readonly List<AzureTtsVoiceModel> _list;

        public ModifyAzureTtsVoicesWindow(List<AzureTtsVoiceModel> list)
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
            textVoice.Text = _list[listBox.SelectedIndex].Voice;
            textLanguage.Text = _list[listBox.SelectedIndex].Language;
        }

        private AzureTtsVoiceModel GetNewModel()
        {
            var model = new AzureTtsVoiceModel();

            if (!string.IsNullOrWhiteSpace(textName.Text))
                model.Name = textName.Text;

            if (!string.IsNullOrWhiteSpace(textVoice.Text))
                model.Voice = textVoice.Text;

            if (!string.IsNullOrWhiteSpace(textLanguage.Text))
                model.Language = textLanguage.Text;

            return model;
        }
        #endregion
    }
}
