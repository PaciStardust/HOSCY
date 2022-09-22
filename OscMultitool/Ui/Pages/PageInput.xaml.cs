using Hoscy;
using Hoscy.Services.Speech;
using Hoscy.Ui.Windows;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageInput.xaml
    /// </summary>
    public partial class PageInput : Page
    {
        public PageInput()
        {
            InitializeComponent();
            RefreshPresets();
        }

        private void Button_Send(object sender, RoutedEventArgs e)
            => SendMessage();

        private string _lastMessage = string.Empty;

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
                ReplaceCaseInsensitive = Config.Input.IgnoreCaps,
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

        private void Button_ChangePresets(object sender, RoutedEventArgs e)
        {
            var window = new ModifyDictionaryWindow("Change Input Presets", "Name", "Text", Config.Input.Presets);
            window.SetDarkMode(true);
            window.ShowDialog();
            RefreshPresets();
        }

        private void RefreshPresets()
            => presetBox.Refresh(Config.Input.Presets.Select(x => x.Key), -1);

        private void TextBox_KeyPressed(object sender, KeyEventArgs e)
        {
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
    }
}
