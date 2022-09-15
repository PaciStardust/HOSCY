using Hoscy.Services.Speech;
using Hoscy.Services.Speech.Utilities;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageOutput.xaml
    /// </summary>
    public partial class PageOutput : Page
    {
        public PageOutput()
        {
            InitializeComponent();
            UpdateTimeoutBoxes();
            LoadComboBoxes();
        }

        #region Textbox
        private void Button_SkipBox(object sender, RoutedEventArgs e)
            => Textbox.Clear();

        private void TextboxDynamicTimeout_Checked(object sender, RoutedEventArgs e)
            => UpdateTimeoutBoxes();

        private void UpdateTimeoutBoxes()
        {
            UiHelper.SetEnabled(optionDefaultTimeout, !textboxDynamicTimeout.IsChecked ?? false);
            UiHelper.SetEnabled(optionDynamicTimeout, textboxDynamicTimeout.IsChecked ?? false);
        }
        #endregion

        #region TTS
        private void LoadComboBoxes()
        {
            //Speakers
            UiHelper.LoadComboBox(speechSpeakerBox, Devices.Speakers.Select(x => x.ProductName), Devices.GetSpeakerIndex(Config.Speech.SpeakerId));
            //Windows Synths
            UiHelper.LoadComboBox(speechWindowsSynthBox, Synthesizing.WindowsSynths.Select(x => x.Description), Synthesizing.GetWindowsSynthIndex(Config.Speech.TtsId));
        }

        private void Button_SkipSpeech(object sender, RoutedEventArgs e)
            => Synthesizing.Skip();

        private void SpeechSpeakerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.Speech.SpeakerId = Devices.Speakers[speechSpeakerBox.SelectedIndex].ProductName;
            Synthesizing.ChangeSpeakers();
        }

        private void SpeechWindowsSynthBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Config.Speech.TtsId = Synthesizing.WindowsSynths[speechWindowsSynthBox.SelectedIndex].Id;
            Synthesizing.ChangeVoice();
        }

        private void Slider_Volume(object sender, RoutedPropertyChangedEventArgs<double> e)
            => Synthesizing.ChangeVolume();
        #endregion
    }
}
