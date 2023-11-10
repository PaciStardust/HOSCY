using Hoscy.Services.Speech;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hoscy.Ui
{
    internal static class ManualInputHelper
    {
        private static string _lastMessage = string.Empty;

        internal static void SendMessage(TextBox textBox)
        {
            var input = textBox.Text.Trim(Environment.NewLine.ToCharArray());

            if (string.IsNullOrWhiteSpace(input))
            {
                textBox.Text = _lastMessage;
                return;
            }

            Logger.Info("Manually input message: " + input);

            var tProcessor = new TextProcessor()
            {
                TriggerCommands = Config.Input.TriggerCommands,
                TriggerReplace = Config.Input.TriggerReplace,
                UseTextbox = Config.Input.UseTextbox,
                UseTts = Config.Input.UseTts,
                AllowTranslate = Config.Input.AllowTranslation
            };
            tProcessor.Process(input);

            _lastMessage = input;
            textBox.Text = string.Empty;
        }

        internal static void KeyPress(TextBox textBox, KeyEventArgs e)
        {
            bool typing = e.Key != Key.Enter && textBox.Text.Length != 0;
            Textbox.EnableTyping(typing);

            if (e.Key == Key.Enter)
                SendMessage(textBox);
        }
    }
}
