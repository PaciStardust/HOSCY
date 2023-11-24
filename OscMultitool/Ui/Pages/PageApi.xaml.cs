using Hoscy.Services.Api;
using Hoscy.Ui.Windows;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageApi.xaml
    /// </summary>
    internal partial class PageApi : Page
    {
        private static bool _changedValuesTranslation = false;
        public PageApi()
        {
            InitializeComponent();
            LoadBoxes();
            UpdateChangedValuesIndicator();
        }

        #region Loading
        private void LoadBoxes()
        {
            translatorApiBox.Load(Config.Api.Presets.Select(x => x.Name), Config.Api.GetPresetIndex(Config.Api.TranslationPreset));
        }
        #endregion

        #region Change Indicators
        private void UpdateChangedValuesIndicator()
        {
            changeIndicatorTranslation.Visibility = _changedValuesTranslation ? Visibility.Visible : Visibility.Hidden;
        }

        private void SetChangedValueTranslation(bool state)
        {
            _changedValuesTranslation = state;
            UpdateChangedValuesIndicator();
        }
        #endregion

        #region Buttons
        private void Button_ModifyPresets(object sender, RoutedEventArgs e)
        {
            var window = new ModifyApiPresetsWindow(Config.Api.Presets);
            window.ShowDialogDark();
            LoadBoxes();
            SetChangedValueTranslation(true);
        }

        private void Button_ReloadTranslation(object sender, RoutedEventArgs e)
        {
            Translator.ReloadClient();
            SetChangedValueTranslation(false);
        }

        private void Button_ModifyMediaFilter(object sender, RoutedEventArgs e)
        {
            var window = new ModifyFiltersWindow("Edit Media Filter", Config.Textbox.MediaFilters);
            window.ShowDialogDark();
        }
        #endregion

        #region SelectionChanged

        private void TranslatorApiBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = translatorApiBox.SelectedIndex;
            if (index == -1 || index >= Config.Api.Presets.Count)
                return;

            Config.Api.TranslationPreset = Config.Api.Presets[translatorApiBox.SelectedIndex].Name;

            if (IsLoaded)
                SetChangedValueTranslation(true);
        }
        #endregion
    }
}
