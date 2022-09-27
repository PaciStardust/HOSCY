using Hoscy.Services.Speech;
using Hoscy.Services.Speech.Recognizers;
using Hoscy.Services.Speech.Utilities;
using Hoscy.Ui.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for TestPage.xaml
    /// </summary>
    public partial class PageSpeech : Page
    {
        private static bool _changedValues = false;

        private static readonly Dictionary<string, RecognizerPerms> _permDict = new()
        {
            { "Windows Recognizer V2", RecognizerWindowsV2.Perms },
            { "Windows Recognizer", RecognizerWindows.Perms },
            { "Vosk AI Recognizer", RecognizerVosk.Perms },
            { "Any-API Recognizer", RecognizerApi.Perms },
            { "Azure API Recognizer", RecognizerAzure.Perms }
        };

        public static RecognizerBase? GetRecognizerFromUi()
            => Config.Speech.ModelName switch
            {
                "Windows Recognizer V2" => new RecognizerWindowsV2(),
                "Windows Recognizer" => new RecognizerWindows(),
                "Vosk AI Recognizer" => new RecognizerVosk(),
                "Any-API Recognizer" => new RecognizerApi(),
                "Azure API Recognizer" => new RecognizerAzure(),
                _ => new RecognizerWindowsV2()
            };

        public PageSpeech()
        {
            InitializeComponent();
            LoadBoxes();
            UpdateRecognizerSelector();

            UpdateRecognizerStatus(null, new(Recognition.IsRecognizerRunning, Recognition.IsRecognizerListening));
            Recognition.RecognitionChanged += UpdateRecognizerStatus;
        }

        #region Loading
        private void UpdateRecognizerStatus(object? sender, RecognitionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                buttonMute.Content = e.Listening ? "Listening" : "Muted";
                buttonMute.Foreground = e.Listening ? UiHelper.ColorValid : UiHelper.ColorInvalid;

                buttonStartStop.Content = e.Running ? "Running" : "Stopped";
                buttonStartStop.Foreground = e.Running ? UiHelper.ColorValid : UiHelper.ColorInvalid;

                if (!e.Running)
                {
                    _changedValues = false;
                    changeIndicator.Visibility = Visibility.Hidden;
                }
            });
        }

        private void LoadBoxes()
        {
            //Microphones
            speechMicrophoneBox.Load(Devices.Microphones.Select(x => x.ProductName), Devices.GetMicrophoneIndex(Config.Speech.MicId));
            //Windows Listeners
            speechWindowsRecognizerBox.Load(Recognition.WindowsRecognizers.Select(x => x.Description), Recognition.GetWindowsListenerIndex(Config.Speech.WinModelId));

            changeIndicator.Visibility = _changedValues ? Visibility.Visible : Visibility.Hidden;
        }

        private void EnableChangeIndicator()
        {
            if (!Recognition.IsRecognizerRunning)
                return;

            changeIndicator.Visibility = Visibility.Visible;
            _changedValues = true;
        }

        /// <summary>
        /// Changes both config value and loads it, 
        /// Updates UI based on selection
        /// </summary>
        private void UpdateRecognizerSelector()
        {
            var oldModelName = Config.Speech.ModelName;

            var keys = _permDict.Keys.ToArray();
            if (recognizerSelector.Items.Count == 0)
                recognizerSelector.ItemsSource = keys;

            //if selection is valid, select it, else find or default
            int recSelIndex = recognizerSelector.SelectedIndex;
            if (recSelIndex != -1 && recSelIndex < keys.Length)
            {
                Config.Speech.ModelName = keys[recognizerSelector.SelectedIndex];
            }
            else
            {
                int index = -1;

                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i] == Config.Speech.ModelName)
                    {
                        index = i;
                        break;
                    }
                }

                index = index == -1 ? 0 : index;
                recognizerSelector.SelectedIndex = index;
                recSelIndex = index;
            }

            var perms = _permDict[keys[recSelIndex]];

            valueRecInfo.Text = perms.Description;
            optionsMic.IsEnabled = perms.UsesMicrophone;
            optionsVosk.IsEnabled = perms.UsesVoskModel;
            optionsWin.IsEnabled = perms.UsesWinRecognizer;

            if (oldModelName != Config.Speech.ModelName)
                EnableChangeIndicator();
        }
        #endregion

        #region Buttons
        private void Button_BrowseVoskModel(object sender, RoutedEventArgs e)
        {
            var oldPath = Config.Speech.VoskModelPath;

            using var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            string folder = result.ToString() == "OK" ? dialog.SelectedPath : string.Empty;
            Config.Speech.VoskModelPath = folder;
            speechVoskPath.Text = folder;

            if (oldPath != Config.Speech.VoskModelPath)
                EnableChangeIndicator();
        }
        private void Button_StartStop(object sender, RoutedEventArgs e)
        {
            if (Recognition.IsRecognizerRunning)
                Recognition.StopRecognizer();
            else
                Recognition.StartRecognizer();
        }

        private void Button_ResetDevice(object sender, RoutedEventArgs e)
        {
            var oldId = Config.Speech.MicId;
            Config.Speech.MicId = string.Empty;

            if (Config.Speech.MicId != oldId)
                EnableChangeIndicator();
        }

        private void Button_Mute(object sender, RoutedEventArgs e)
            => Recognition.SetListening(!Recognition.IsRecognizerListening);

        private void Button_OpenNoiseFilter(object sender, RoutedEventArgs e)
        {
            var window = new ModifyListWindow("Edit Noise Filter", "Noise Text", Config.Speech.NoiseFilter);
            window.SetDarkMode(true);
            window.ShowDialog();
            RecognizerBase.UpdateDenoiseRegex();
        }
        private void Button_OpenReplacements(object sender, RoutedEventArgs e)
        {
            var window = new ModifyReplacementsWindow("Edit Replacements", Config.Speech.Replacements);
            window.SetDarkMode(true);
            window.ShowDialog();
        }
        private void Button_OpenShortcuts(object sender, RoutedEventArgs e)
        {
            var window = new ModifyReplacementsWindow("Edit Shortcuts", Config.Speech.Shortcuts);
            window.SetDarkMode(true);
            window.ShowDialog();
        }
        #endregion

        #region SelectionChanged
        private void SpeechMicrophoneBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldId = Config.Speech.MicId;

            var index = speechMicrophoneBox.SelectedIndex;
            if (index != -1 && index < Devices.Microphones.Count)
                Config.Speech.MicId = Devices.Microphones[speechMicrophoneBox.SelectedIndex].ProductName;

            if (oldId != Config.Speech.MicId)
                EnableChangeIndicator();
        }

        private void SpeechWindowsRecognizerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldId = Config.Speech.WinModelId;

            var index = speechWindowsRecognizerBox.SelectedIndex;
            if (index != -1 && index < Recognition.WindowsRecognizers.Count)
                Config.Speech.WinModelId = Recognition.WindowsRecognizers[speechWindowsRecognizerBox.SelectedIndex].Id;

            if (oldId != Config.Speech.WinModelId)
                EnableChangeIndicator();
        }

        private void RecognizerSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateRecognizerSelector();

        private void TextPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            EnableChangeIndicator();
            e.Handled = true;
        }
        #endregion
    }
}
