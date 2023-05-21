using Hoscy.Services.Speech;
using Hoscy.Services.Speech.Recognizers;
using Hoscy.Services.Speech.Utilities;
using Hoscy.Ui.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Whisper;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for TestPage.xaml
    /// </summary>
    internal partial class PageSpeech : Page
    {
        private static bool _changedValues = false;

        private static readonly IReadOnlyDictionary<string, RecognizerPerms> _permDict = new Dictionary<string, RecognizerPerms>()
        {
            { "Vosk AI Recognizer", RecognizerVosk.Perms },
            { "Windows Recognizer V2", RecognizerWindowsV2.Perms },
            { "Windows Recognizer", RecognizerWindows.Perms },
            { "Any-API Recognizer", RecognizerApi.Perms },
            { "Azure API Recognizer", RecognizerAzure.Perms },
            { "Whisper Recognizer", RecognizerWhisper.Perms }
        };

        internal static RecognizerBase? GetRecognizerFromUi()
            => Config.Speech.ModelName switch
            {
                "Vosk AI Recognizer" => new RecognizerVosk(),
                "Windows Recognizer V2" => new RecognizerWindowsV2(),
                "Windows Recognizer" => new RecognizerWindows(),
                "Any-API Recognizer" => new RecognizerApi(),
                "Azure API Recognizer" => new RecognizerAzure(),
                "Whisper Recognizer" => new RecognizerWhisper(),
                _ => new RecognizerVosk()
            };

        public PageSpeech()
        {
            InitializeComponent();
            LoadBoxes();
            UpdateRecognizerSelector();

            UpdateRecognizerStatus(null, new(Recognition.IsRunning, Recognition.IsListening));
            Recognition.RecognitionChanged += UpdateRecognizerStatus;

            changeIndicator.Visibility = _changedValues ? Visibility.Visible : Visibility.Hidden;
        }

        #region Loading
        /// <summary>
        /// Updates the recognizer status if it is changed in the recognizer
        /// </summary>
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

        /// <summary>
        /// Loads data into UI boxes
        /// </summary>
        private void LoadBoxes()
        {
            //Microphones
            speechMicrophoneBox.Load(Devices.Microphones.Select(x => x.ProductName), Devices.GetMicrophoneIndex(Config.Speech.MicId));
            //Windows Listeners
            windowsRecognizerBox.Load(Devices.WindowsRecognizers.Select(x => x.Description), Devices.GetWindowsListenerIndex(Config.Speech.WinModelId));
            //AnyAPI presrt
            anyApiBox.Load(Config.Api.Presets.Select(x => x.Name), Config.Api.GetIndex(Config.Api.RecognitionPreset));

            UpdateVoskRecognizerBox();
            UpdateWhisperRecognizerBox();
            LoadWhisperLanguageBox();
        }

        /// <summary>
        /// Tries to enable the UI change indicator, will fail if recognizer is not running
        /// </summary>
        private void TryEnableChangeIndicator()
        {
            if (!Recognition.IsRunning)
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
            optionsMic.Visibility = perms.UsesMicrophone ? Visibility.Visible : Visibility.Collapsed;
            optionsVosk.Visibility = perms.Type == RecognizerType.Vosk ? Visibility.Visible : Visibility.Collapsed;
            optionsWin.Visibility = perms.Type == RecognizerType.Windows ? Visibility.Visible : Visibility.Collapsed;
            optionsAnyApi.Visibility = perms.Type == RecognizerType.AnyApi ? Visibility.Visible : Visibility.Collapsed;
            optionsAzure.Visibility = perms.Type == RecognizerType.Azure ? Visibility.Visible : Visibility.Collapsed;
            optionsWhisper.Visibility = perms.Type == RecognizerType.Whisper ? Visibility.Visible : Visibility.Collapsed;

            if (oldModelName != Config.Speech.ModelName)
                TryEnableChangeIndicator();
        }

        /// <summary>
        /// Updates contents of the Vosk Recognizer dropdown
        /// </summary>
        private void UpdateVoskRecognizerBox()
            => voskModelBox.UpdateModelBox(Config.Speech.VoskModels, Config.Speech.VoskModelCurrent);

        /// <summary>
        /// Updates contents of the Whisper Recognizer dropdown
        /// </summary>
        private void UpdateWhisperRecognizerBox()
            => whisperModelBox.UpdateModelBox(Config.Speech.WhisperModels, Config.Speech.WhisperModelCurrent, false);

        private void LoadWhisperLanguageBox()
        {
            var languages = Enum.GetValues(typeof(eLanguage)).Cast<eLanguage>().ToList();
            if (languages == null)
            {
                Logger.Error("Failed to grab enum values for whisper languages", false);
                return;
            }

            var sortedLanguages = languages.OrderBy(x => x.ToString()).ToList();
            var languageIndex = sortedLanguages.IndexOf(Config.Speech.WhisperLanguage);
            if (languageIndex == -1)
            {
                Logger.Error("Failed to grab whisper language index corresponding to config value", false);
                return;
            }

            whisperLanguageBox.Load(sortedLanguages, languageIndex);
        }
        #endregion

        #region Buttons
        private async void Button_StartStop(object sender, RoutedEventArgs e)
        {
            if (Recognition.IsRunning)
                Recognition.StopRecognizer();
            else
            {
                buttonStartStop.Content = "Starting";
                buttonStartStop.Foreground = UiHelper.ColorFront;
                await Task.Run(async() => await Task.Delay(10));
                Recognition.StartRecognizer();
            }
        }

        private void Button_ResetDevice(object sender, RoutedEventArgs e)
        {
            var oldId = Config.Speech.MicId;
            Config.Speech.MicId = string.Empty;

            if (Config.Speech.MicId != oldId)
                TryEnableChangeIndicator();
        }

        private void Button_Mute(object sender, RoutedEventArgs e)
            => Recognition.SetListening(!Recognition.IsListening);

        private void Button_OpenNoiseFilter(object sender, RoutedEventArgs e)
        {
            UiHelper.OpenListEditor("Edit Noise Filter", "Noise Text", Config.Speech.NoiseFilter);
            Recognition.UpdateDenoiseRegex();
        }
        private void Button_OpenReplacements(object sender, RoutedEventArgs e)
        {
            var window = new ModifyReplacementDataWindow("Edit Replacements", Config.Speech.Replacements);
            window.ShowDialogDark();
            TextProcessor.UpdateReplacementDataHandlers();
        }
        private void Button_OpenShortcuts(object sender, RoutedEventArgs e)
        {
            var window = new ModifyReplacementDataWindow("Edit Shortcuts", Config.Speech.Shortcuts);
            window.ShowDialogDark();
            TextProcessor.UpdateReplacementDataHandlers();
        }

        private void Button_EditVoskModels(object sender, RoutedEventArgs e)
        {
            UiHelper.OpenDictionaryEditor("Edit Vosk AI Models", "Model name", "Model folder", Config.Speech.VoskModels);
            UpdateVoskRecognizerBox();
        }

        private void Button_EditWhisperModels(object sender, RoutedEventArgs e)
        {
            UiHelper.OpenDictionaryEditor("Edit Whisper AI Models", "Model name", "Model folder", Config.Speech.WhisperModels);
            UpdateWhisperRecognizerBox();
        }

        private void Button_EditAzurePhrases(object sender, RoutedEventArgs e)
        {
            UiHelper.OpenListEditor("Edit Phrases", "Phrase", Config.Api.AzurePhrases, "New Phrase");
            TryEnableChangeIndicator();
        }

        private void Button_EditAzureLanguages(object sender, RoutedEventArgs e)
        {
            UiHelper.OpenListEditor("Edit Languages", "Language", Config.Api.AzureRecognitionLanguages, "New Language");
            TryEnableChangeIndicator();
        }

        private void Button_EditWhisperNoiseWhitelist(object sender, RoutedEventArgs e)
        {
            var window = new ModifyFiltersWindow("Edit Noise Whitelist", Config.Speech.WhisperNoiseWhitelist);
            window.ShowDialogDark();
        }
        #endregion

        #region SelectionChanged
        private void RecognizerSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateRecognizerSelector();

        private void SpeechMicrophoneBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldId = Config.Speech.MicId;

            var index = speechMicrophoneBox.SelectedIndex;
            if (index != -1 && index < Devices.Microphones.Count)
                Config.Speech.MicId = Devices.Microphones[speechMicrophoneBox.SelectedIndex].ProductName;

            if (oldId != Config.Speech.MicId)
                TryEnableChangeIndicator();
        }

        private void WindowsRecognizerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldId = Config.Speech.WinModelId;

            var index = windowsRecognizerBox.SelectedIndex;
            if (index != -1 && index < Devices.WindowsRecognizers.Count)
                Config.Speech.WinModelId = Devices.WindowsRecognizers[windowsRecognizerBox.SelectedIndex].Id;

            if (oldId != Config.Speech.WinModelId)
                TryEnableChangeIndicator();
        }

        private void VoskModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string oldModelName = Config.Speech.VoskModelCurrent;

            var index = voskModelBox.SelectedIndex;
            if (index != -1 && index < Config.Speech.VoskModels.Count)
                Config.Speech.VoskModelCurrent = Config.Speech.VoskModels.Keys.ToArray()[index];

            if (oldModelName != Config.Speech.VoskModelCurrent)
                TryEnableChangeIndicator();
        }

        private void WhisperModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string oldModelName = Config.Speech.WhisperModelCurrent;

            var index = whisperModelBox.SelectedIndex;
            if (index != -1 && index < Config.Speech.WhisperModels.Count)
                Config.Speech.WhisperModelCurrent = Config.Speech.WhisperModels.Keys.ToArray()[index];

            if (oldModelName != Config.Speech.WhisperModelCurrent)
                TryEnableChangeIndicator();
        }

        private void AnyApiBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string oldAnyApiName = Config.Api.RecognitionPreset;

            int index = anyApiBox.SelectedIndex;
            if (index == -1 || index >= Config.Api.Presets.Count)
                return;

            Config.Api.RecognitionPreset = Config.Api.Presets[anyApiBox.SelectedIndex].Name;

            if (oldAnyApiName != Config.Api.RecognitionPreset)
                TryEnableChangeIndicator();
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
            => TryEnableChangeIndicator();

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
            => TryEnableChangeIndicator();

        private void WhisperLanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldLanguage = Config.Speech.WhisperLanguage;
            eLanguage? newLanguage = (eLanguage)whisperLanguageBox.SelectedItem;

            if (newLanguage == null)
            {
                Logger.Error("Failed to assign selected whisper language to config");
                return;
            }

            Config.Speech.WhisperLanguage = newLanguage.Value;

            if (oldLanguage != Config.Speech.WhisperLanguage)
                TryEnableChangeIndicator();
        }
        #endregion
    }
}
