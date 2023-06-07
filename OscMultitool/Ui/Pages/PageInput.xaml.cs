using Hoscy.Services.Speech;
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
            => SendMessage();

        private void Button_ChangePresets(object sender, RoutedEventArgs e)
        {
            UiHelper.OpenDictionaryEditor("Edit Input Presets", "Name", "Text", Config.Input.Presets);
            RefreshPresets();
        }
        #endregion

        private static string _lastMessage = string.Empty;

        /// <summary>
        /// Sends data to processor depending on checked options
        /// </summary>
        private void SendMessage()
        {
            var message = textBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                if (!string.IsNullOrWhiteSpace(_lastMessage))
                    textBox.Text = _lastMessage;
                return;
            }

            Logger.Info("Manually input message: " + message);

            var tProcessor = new TextProcessor()
            {
                TriggerCommands = Config.Input.TriggerCommands,
                TriggerReplace = Config.Input.TriggerReplace,
                UseTextbox = Config.Input.UseTextbox,
                UseTts = Config.Input.UseTts,
                AllowTranslate = Config.Input.AllowTranslation
            };
            tProcessor.Process(message);

            _lastMessage = message;
            textBox.Text = string.Empty;
        }

        #region Other
        private void RefreshPresets()
            => presetBox.Load(Config.Input.Presets.Select(x => x.Key), -1);

        private void TextBox_KeyPressed(object sender, KeyEventArgs e)
        {
            bool typing = e.Key != Key.Enter && textBox.Text.Length != 0;
            Textbox.EnableTyping(typing);

            if (e.Key == Key.Enter)
                SendMessage();
        }

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
