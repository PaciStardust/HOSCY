using Hoscy.Services.OscControl;
using Hoscy.Services.Speech;
using Hoscy.Ui.Windows;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageInfo.xaml
    /// </summary>
    internal partial class PageInfo : Page
    {
        internal static PageInfo? Instance { get; private set; } = null;

        private static string _sendStatus = "No message sent since opening";
        private static string _message = "No message sent since opening";
        private static string _notification = "No notification since opening";

        public PageInfo()
        {
            InitializeComponent();

            sendStatus.Text = _sendStatus;
            message.Text = _message;
            notification.Text = _notification;

            Instance = this;

            UpdateRecognizerStatus(null, new(Recognition.IsRunning, Recognition.IsListening));
            Recognition.RecognitionChanged += UpdateRecognizerStatus;
        }

        #region Buttons
        private void Button_Mute(object sender, RoutedEventArgs e)
            => Recognition.SetListening(!Recognition.IsListening);

        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            SetCommandMessage("Clear");
            Textbox.Clear();
            Synthesizing.Skip();
            OscDataHandler.SetAfkTimer(false);
        }

        private const string _perfTip = "\n\nTip: Unfocus HOSCY for better performance";
        private async void Button_Start(object sender, RoutedEventArgs e)
        {
            if (Recognition.IsRunning)
            {
                Recognition.StopRecognizer();
                SetRecStatus("Recognizer has stopped" + _perfTip);
            }
            else
            {
                startButton.Content = "Starting";
                startButton.Foreground = UiHelper.ColorFront;
                SetRecStatus("Recognizer is starting" + _perfTip);
                await Task.Run(async () => await Task.Delay(10));
                if (Recognition.StartRecognizer())
                    SetRecStatus("Recognizer has started" + _perfTip);
                else
                    SetRecStatus("Recognizer failed to start" + _perfTip);
            }
        }

        private void Button_History(object sender, RoutedEventArgs e)
        {
            var window = new DisplayChatHistoryWindow();
            window.Show();
        }
        #endregion

        #region Text Setters
        /// <summary>
        /// Updates the mic status indicator based on the recognizer status
        /// </summary>
        private static void UpdateRecognizerStatus(object? sender, RecognitionChangedEventArgs e)
        {
            Instance?.Dispatcher.Invoke(() =>
            {
                Instance.muteButton.Content = e.Listening ? "Listening" : "Muted";
                Instance.muteButton.Foreground = e.Listening ? UiHelper.ColorValid : UiHelper.ColorInvalid;

                Instance.startButton.Content = e.Running ? "Running" : "Stopped";
                Instance.startButton.Foreground = e.Running ? UiHelper.ColorValid : UiHelper.ColorInvalid;
            });
        }

        /// <summary>
        /// Sets the message as a command
        /// </summary>
        /// <param name="message">Message to display</param>
        internal static void SetCommandMessage(string message, bool success = true)
        {
            _sendStatus = success ? "Executed command" : "Failed to execute command";
            _message = message;

            Instance?.Dispatcher.Invoke(() =>
            {
                Instance.sendStatus.Text = _sendStatus;
                Instance.message.Text = _message;
            });
        }

        /// <summary>
        /// Sets the message as a command
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="textbox">Did it send via Textbox</param>
        /// <param name="tts">Did it send via TTS</param>
        internal static void SetMessage(string message, bool textbox, bool tts) 
        {
            var add = "Nothing";
            if (textbox && tts)
                add = "Textbox and TTS";
            else if (textbox)
                add = "Textbox";
            else if (tts)
                add = "TTS";

            _sendStatus = "Sent via " + add;
            _message = message;

            Instance?.Dispatcher.Invoke(() =>
            {
                Instance.sendStatus.Text = _sendStatus;
                Instance.message.Text = _message;
            });
        }

        internal static void SetNotification(string message, NotificationType type)
        {
            _notification = $"[{type}] {message}";

            Instance?.Dispatcher.Invoke(() =>
            {
                Instance.notification.Text = _notification;
            });
        }

        private void SetRecStatus(string text)
        {
            _sendStatus = "Recognition status";
            _message = text;

            sendStatus.Text = _sendStatus;
            message.Text = _message;
        }
        #endregion
    }
}
