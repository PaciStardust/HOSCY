using OscMultitool.Services.Api;
using OscMultitool.Ui.Windows;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OscMultitool.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageApi.xaml
    /// </summary>
    public partial class PageApi : Page
    {
        public PageApi()
        {
            InitializeComponent();
            LoadBoxes();
        }

        private void LoadBoxes()
        {
            LoadPresetBox(translatorApiBox, Config.Api.TranslationPreset);
            LoadPresetBox(recognitionApiBox, Config.Api.RecognitionPreset);
        }

        #region Buttons
        private void Button_ModifyPresets(object sender, RoutedEventArgs e)
        {
            var window = new ModifyApiPresetsWindow("Edit API Presets", Config.Api.Presets);
            window.ShowDialog();
            LoadBoxes();
        }

        private void Button_ReloadTranslation(object sender, RoutedEventArgs e)
            => Translation.ReloadClient();
        #endregion

        #region SelectionChanged
        private void RecognitionApiBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = recognitionApiBox.SelectedIndex;
            if (index == -1 || index >= Config.Api.Presets.Count)
                return;

            Config.Api.RecognitionPreset = Config.Api.Presets[recognitionApiBox.SelectedIndex].Name;
        }

        private void TranslatorApiBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = translatorApiBox.SelectedIndex;
            if (index == -1 || index >= Config.Api.Presets.Count)
                return;

            Config.Api.TranslationPreset = Config.Api.Presets[translatorApiBox.SelectedIndex].Name;
        }
        #endregion

        private static void LoadPresetBox(ComboBox box, string name)
            => UiHelper.LoadComboBox(box, Config.Api.Presets.Select(x => x.Name), Config.Api.GetIndex(name));
    }
}
