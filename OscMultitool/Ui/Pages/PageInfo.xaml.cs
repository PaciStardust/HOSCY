using Hoscy.Services.Speech;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hoscy.Ui.Pages
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
            UpdateRecognizerStatus();
        }

        /// <summary>
        /// Updates the mic status indicator based on the recognizer status
        /// </summary>
        public static void UpdateRecognizerStatus()
        {
            var micStatus = Recognition.IsRecognizerListening;
            var recStatus = Recognition.IsRecognizerRunning;

            Instance.Dispatcher.Invoke(() =>
            {
                Instance.muteButton.Content = micStatus ? "Listening" : "Muted";
                Instance.muteButton.Foreground = micStatus ? UiHelper.ColorValid : UiHelper.ColorInvalid;

                Instance.startButton.Content = recStatus ? "Running" : "Stopped";
                Instance.startButton.Foreground = recStatus ? UiHelper.ColorValid : UiHelper.ColorInvalid;
            });
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

        private void Button_Start(object sender, RoutedEventArgs e)
        {
            if (Recognition.IsRecognizerRunning)
                Recognition.StopRecognizer();
            else
                Recognition.StartRecognizer();

            UpdateRecognizerStatus();
        }
    }
}
