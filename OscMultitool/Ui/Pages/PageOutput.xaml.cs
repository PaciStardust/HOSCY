using Hoscy.Services.OscControl;
using Hoscy.Services.Speech;
using Hoscy.Services.Speech.Utilities;
using Hoscy.Ui.Windows;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageOutput.xaml
    /// </summary>
    internal partial class PageOutput : Page
    {
        public PageOutput()
        {
            InitializeComponent();
            UpdateTimeoutBoxes();
            LoadComboBoxes();
            UpdateVolumeText();
        }

        #region Textbox
        private void Button_SkipBox(object sender, RoutedEventArgs e)
            => Textbox.Clear();

        private void TextboxDynamicTimeout_Checked(object sender, RoutedEventArgs e)
            => UpdateTimeoutBoxes();

        private void UpdateTimeoutBoxes()
        {
            optionDefaultTimeout.IsEnabled = !textboxDynamicTimeout.IsChecked ?? false;
            optionDynamicTimeout.IsEnabled = textboxDynamicTimeout.IsChecked ?? false;
        }
        #endregion

        #region TTS
        private void LoadComboBoxes()
        {
            //Speakers
            speechSpeakerBox.Load(Devices.Speakers.Select(x => x.ProductName), Devices.GetSpeakerIndex(Config.Speech.SpeakerId));
            //Windows Synths
            speechWindowsSynthBox.Load(Synthesizing.WindowsSynths.Select(x => x.Description), Synthesizing.GetWindowsSynthIndex(Config.Speech.TtsId));
        }

        private void Button_SkipSpeech(object sender, RoutedEventArgs e)
            => Synthesizing.Skip();

        private void SpeechSpeakerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.Speech.SpeakerId = Devices.Speakers[speechSpeakerBox.SelectedIndex].ProductName;
            Synthesizing.ChangeSpeakers();
        }

        private void Button_ResetDevice(object sender, RoutedEventArgs e)
        {
            Config.Speech.SpeakerId = string.Empty;
            LoadComboBoxes();
        }

        private void SpeechWindowsSynthBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.Speech.TtsId = Synthesizing.WindowsSynths[speechWindowsSynthBox.SelectedIndex].Id;
            Synthesizing.ChangeVoice();
        }

        private void Slider_Volume(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (volumeSlider != null)
            {
                UpdateVolumeText();
                Synthesizing.ChangeVolume();
            }
        }

        private void UpdateVolumeText()
        {
            if (volumeLabel != null)
                volumeLabel.Content = $"Speech volume ({(int)Math.Round(volumeSlider.Value * 100)}%)";
        }
        #endregion

        #region Other
        private void Button_ModifyMediaFilter(object sender, RoutedEventArgs e)
        {
            var window = new ModifyListWindow("Edit Media Filters", "Filter", Config.Textbox.MediaFilter);
            window.SetDarkMode(true);
            window.ShowDialog();
        }
        #endregion
    }
}
