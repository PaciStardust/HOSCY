using Hoscy.Services.Api;
using Hoscy.Ui.Windows;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageApi.xaml
    /// </summary>
    public partial class PageApi : Page
    {
        private static bool _changedValuesTranslation = false;
        private static bool _changedValuesSynthesizer = false;
        public PageApi()
        {
            InitializeComponent();
            LoadBoxes();
            UpdateChangedValuesIndicator();
        }

        private void LoadBoxes()
        {
            LoadPresetBox(translatorApiBox, Config.Api.TranslationPreset);
            LoadPresetBox(recognitionApiBox, Config.Api.RecognitionPreset);
            UpdateAzureVoiceBox();
        }

        #region Change Indicators
        private void UpdateChangedValuesIndicator()
        {
            changeIndicatorTranslation.Visibility = _changedValuesTranslation ? Visibility.Visible : Visibility.Hidden;
            changeIndicatorSynthesizer.Visibility = _changedValuesSynthesizer ? Visibility.Visible : Visibility.Hidden;
        }

        private void SetChangedValueTranslation(bool state)
        {
            _changedValuesTranslation = state;
            UpdateChangedValuesIndicator();
        }

        private void SetChangedValueSynthesizer(bool state)
        {
            _changedValuesSynthesizer = state;
            UpdateChangedValuesIndicator();
        }
        #endregion

        #region Buttons
        private void Button_ModifyPresets(object sender, RoutedEventArgs e)
        {
            var window = new ModifyApiPresetsWindow("Edit API Presets", Config.Api.Presets);
            window.SetDarkMode(true);
            window.ShowDialog();
            LoadBoxes();
            SetChangedValueTranslation(true);
        }

        private void Button_ReloadTranslation(object sender, RoutedEventArgs e)
        {
            Translation.ReloadClient();
            SetChangedValueTranslation(false);
        }

        private void Button_ReloadSynthesizer(object sender, RoutedEventArgs e)
        {
            Synthesizer.ReloadClient();
            SetChangedValueSynthesizer(false);
        }

        private void Button_EditPhrases(object sender, RoutedEventArgs e)
            => UiHelper.OpenListEditor("Edit phrases", "Phrase", Config.Api.AzurePhrases, "New Phrase");

        private void Button_EditLanguages(object sender, RoutedEventArgs e)
            => UiHelper.OpenListEditor("Edit languages", "Language", Config.Api.AzureRecognitionLanguages, "New Language");

        private void Button_EditAzureVoices(object sender, RoutedEventArgs e)
        {
            UiHelper.OpenDictionaryEditor("Edit Azure Voices", "Voice Identifier", "Voice Name", Config.Api.AzureVoices);
            UpdateAzureVoiceBox();
        }
        #endregion

        #region SelectionChanged
        private void RecognitionApiBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = recognitionApiBox.SelectedIndex;
            if (index == -1 || index >= Config.Api.Presets.Count)
                return;

            Config.Api.RecognitionPreset = Config.Api.Presets[recognitionApiBox.SelectedIndex].Name;
        }

        private bool _translatorApiBoxFirstChange = true; //This is to stop indicator from showing when page loads
        private void TranslatorApiBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = translatorApiBox.SelectedIndex;
            if (index == -1 || index >= Config.Api.Presets.Count)
                return;

            Config.Api.TranslationPreset = Config.Api.Presets[translatorApiBox.SelectedIndex].Name;

            if (_translatorApiBoxFirstChange)
                _translatorApiBoxFirstChange = false;
            else
                SetChangedValueTranslation(true);
        }

        private void AzureVoiceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string oldVoiceName = Config.Api.AzureVoiceCurrent;

            var index = azureVoiceBox.SelectedIndex;
            if (index != -1 && index < Config.Api.AzureVoices.Count)
                Config.Api.AzureVoiceCurrent = Config.Api.AzureVoices.Keys.ToArray()[index];

            if (oldVoiceName != Config.Api.AzureVoiceCurrent)
            {
                _changedValuesSynthesizer = true;
                UpdateChangedValuesIndicator();
            }
                
        }
        #endregion

        private static void LoadPresetBox(ComboBox box, string name)
            => box.Load(Config.Api.Presets.Select(x => x.Name), Config.Api.GetIndex(name));

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
            => SetChangedValueSynthesizer(true);

        private void UpdateAzureVoiceBox()
        {
            var voices = Config.Api.AzureVoices;

            //Checking for availability of current model in dropdown
            int index = -1;
            var keyArray = voices.Keys.ToArray();
            for (int i = 0; i < keyArray.Length; i++)
            {
                if (Config.Api.AzureVoiceCurrent == keyArray[i])
                {
                    index = i;
                    break;
                }
            }

            //Clearing, very cool
            azureVoiceBox.ItemsSource = null;
            foreach (var item in azureVoiceBox.Items)
                azureVoiceBox.Items.Remove(item);
            azureVoiceBox.Items.Refresh();

            azureVoiceBox.Load(voices.Keys, index, true);
        }
    }
}
