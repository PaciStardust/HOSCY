﻿using Hoscy.Ui.Windows;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageInput.xaml
    /// </summary>
    internal partial class PageInput : Page
    {
        public PageInput()
        {
            InitializeComponent();
            RefreshPresets();
        }

        #region Buttons
        private void Button_Send(object sender, RoutedEventArgs e)
            => ManualInputHelper.SendMessage(textBox);

        private void Button_ChangePresets(object sender, RoutedEventArgs e)
        {
            UiHelper.OpenDictionaryEditor("Modify Input Presets", "Name", "Text", Config.Input.Presets);
            RefreshPresets();
        }

        private void Button_History(object sender, RoutedEventArgs e)
        {
            var window = new DisplayChatHistoryWindow();
            window.Show();
        }
        #endregion

        #region Other
        private void RefreshPresets()
            => presetBox.Load(Config.Input.Presets.Select(x => x.Key), -1);

        private void TextBox_KeyPressed(object sender, KeyEventArgs e)
            => ManualInputHelper.KeyPress(textBox, e);

        private void PresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (presetBox.SelectedIndex == -1)
                return;

            var key = presetBox.SelectedValue.ToString();

            if (string.IsNullOrWhiteSpace(key))
                return;

            if (Config.Input.Presets.ContainsKey(key))
                textBox.Text = Config.Input.Presets[key];

            presetBox.SelectedIndex = -1;
        }
        #endregion
    }
}
