using OscMultitool.Services.Speech;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OscMultitool.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageInfo.xaml
    /// </summary>
    public partial class PageInfo : Page
    {
        public static PageInfo Instance { get; private set; } = new();

        public PageInfo()
        {
            InitializeComponent();

            Instance = this;
            UpdateMicStatus();
        }

        /// <summary>
        /// Updates the mic status indicator based on the recognizer status
        /// </summary>
        public static void UpdateMicStatus()
        {
            var text = Recognition.IsRecognizerListening ? "Listening" : "Muted";

            Instance.Dispatcher.Invoke(() =>
            {
                Instance.muteButton.Content = text;
                Instance.muteButton.Foreground = new SolidColorBrush(GetColorFromStatus());
            });
        }

        private static Color GetColorFromStatus()
        {
            if (!Recognition.IsRecognizerRunning)
                return Colors.White;

            return Recognition.IsRecognizerListening ? Colors.LightGreen : Colors.IndianRed;
        }

        /// <summary>
        /// Sets the message as a command
        /// </summary>
        /// <param name="message">Message to display</param>
        public static void SetCommandMessage(string message)
        {
            Instance.Dispatcher.Invoke(() =>
            {
                Instance.sendStatus.Text = "Executed command";
                Instance.message.Text = message;
            });
        }

        /// <summary>
        /// Sets the message as a command
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="textbox">Did it send via Textbox</param>
        /// <param name="tts">Did it send via TTS</param>
        public static void SetMessage(string message, bool textbox, bool tts)
        {
            var add = "Nothing";
            if (textbox && tts)
                add = "Textbox and TTS";
            else if (textbox)
                add = "Textbox";
            else if (tts)
                add = "TTS";

            Instance.Dispatcher.Invoke(() =>
            {
                Instance.sendStatus.Text = "Sent via " + add;
                Instance.message.Text = message;
            });
        }

        public static void SetNotification(string message, NotificationType type)
        {
            Instance.Dispatcher.Invoke(() =>
            {
                Instance.notification.Text = $"[{type}] {message}";
            });
        }

        private void Button_Mute(object sender, RoutedEventArgs e)
            => Recognition.SetListening(!Recognition.IsRecognizerListening);

        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            SetCommandMessage("Clear");
            Textbox.Clear();
            Synthesizing.Skip();
        }
    }
}
