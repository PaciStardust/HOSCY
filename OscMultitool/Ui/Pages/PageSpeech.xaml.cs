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
                _ => null
            };

        public PageSpeech()
        {
            InitializeComponent();
            LoadBoxes();
            UpdateRecognizerSelector();
            SetButtonTexts();
        }

        #region Loading
        private void SetButtonTexts()
        {
            buttonStartStop.Content = Recognition.IsRecognizerRunning ? "Stop" : "Start";
            buttonMute.Content = Recognition.IsRecognizerListening ? "Listening" : "Muted";
        }

        private void LoadBoxes()
        {
            //Microphones
            UiHelper.LoadComboBox(speechMicrophoneBox, Devices.Microphones.Select(x => x.ProductName), Devices.GetMicrophoneIndex(Config.Speech.MicId));
            //Windows Listeners
            UiHelper.LoadComboBox(speechWindowsRecognizerBox, Recognition.WindowsRecognizers.Select(x => x.Description), Recognition.GetWindowsListenerIndex(Config.Speech.WinModelId));
        }

        /// <summary>
        /// Changes both config value and loads it, 
        /// Updates UI based on selection
        /// </summary>
        private void UpdateRecognizerSelector()
        {
            var keys = _permDict.Keys.ToArray();
            if (recognizerSelector.Items.Count == 0)
                recognizerSelector.ItemsSource = keys;

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

            optionsMic.IsEnabled = perms.UsesMicrophone;
            optionsVosk.IsEnabled = perms.UsesVoskModel;
            optionsWin.IsEnabled = perms.UsesWinRecognizer;
        }
        #endregion

        #region Buttons
        private void Button_BrowseVoskModel(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            string folder = result.ToString() == "OK" ? dialog.SelectedPath : string.Empty;
            Config.Data.Speech.VoskModelPath = folder;
            speechVoskPath.Text = folder;
        }
        private void Button_StartStop(object sender, RoutedEventArgs e)
        {
            if (Recognition.IsRecognizerRunning)
                Recognition.StopRecognizer();
            else
                Recognition.StartRecognizer();

            SetButtonTexts();
        }

        private void Button_Mute(object sender, RoutedEventArgs e)
        {
            Recognition.SetListening(!Recognition.IsRecognizerListening);
            SetButtonTexts();
        }

        private void Button_OpenNoiseFilter(object sender, RoutedEventArgs e)
        {
            var window = new ModifyListWindow("Edit Noise Filter", "Noise Text", Config.Speech.NoiseFilter);
            window.ShowDialog();
            RecognizerBase.UpdateDenoiseRegex();
        }
        private void Button_OpenReplacements(object sender, RoutedEventArgs e)
        {
            var window = new ModifyDictionaryWindow("Edit Replacements", "Text", "Replacement", Config.Speech.Replacements);
            window.ShowDialog();
        }
        private void Button_OpenShortcuts(object sender, RoutedEventArgs e)
        {
            var window = new ModifyDictionaryWindow("Edit Shortcuts", "Text", "Replacement", Config.Speech.Shortcuts);
            window.ShowDialog();
        }
        #endregion

        #region SelectionChanged
        private void SpeechMicrophoneBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = speechMicrophoneBox.SelectedIndex;
            if (index != -1 && index < Devices.Microphones.Count)
                Config.Speech.MicId = Devices.Microphones[speechMicrophoneBox.SelectedIndex].ProductName;
        }

        private void SpeechWindowsRecognizerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = speechWindowsRecognizerBox.SelectedIndex;
            if (index != -1 && index < Recognition.WindowsRecognizers.Count)
                Config.Speech.WinModelId = Recognition.WindowsRecognizers[speechWindowsRecognizerBox.SelectedIndex].Id;
        }

        private void RecognizerSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateRecognizerSelector();
        #endregion
    }
}
