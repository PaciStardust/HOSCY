using Hoscy.Services.Api;
using Hoscy.Services.Speech;
using Hoscy.Services.Speech.Utilities;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageDebug.xaml
    /// </summary>
    public partial class PageConfig : Page
    {
        public PageConfig()
        {
            InitializeComponent();
            versionText.Content = Config.GetVersion();
        }

        #region Buttons
        private void Button_OpenLogFilter(object sender, RoutedEventArgs e)
            => UiHelper.OpenListEditor("Edit Logging Filter", "Log Text", Config.Debug.LogFilter);

        private void Button_ReloadDevices(object sender, RoutedEventArgs e)
        {
            if (Recognition.IsRecognizerRunning)
                Recognition.StopRecognizer();

            Devices.ForceReload();
        }
        private void Button_OpenDocs(object sender, RoutedEventArgs e)
            => UiHelper.StartProcess(Config.Github);
        private void Button_OpenConfig(object sender, RoutedEventArgs e)
            => UiHelper.StartProcess(Config.ResourcePath);
        private void Button_CheckUpdate(object sender, RoutedEventArgs e)
            => HoscyClient.CheckForUpdates();
        private void Button_SaveConfig(object sender, RoutedEventArgs e)
            => Config.SaveConfig();
        private void Button_ReloadMedia(object sender, RoutedEventArgs e)
            => Media.StartMediaDetection();
        #endregion
    }
}
