using Hoscy.Services.Speech;
using Hoscy.Services.Speech.Utilities;
using Hoscy.Ui.Windows;
using System;
using System.Diagnostics;
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
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            versionText.Content = "V." + (assembly != null ? FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion : "Version Unknown");
        }

        private void Button_OpenLogFilter(object sender, RoutedEventArgs e)
        {
            var window = new ModifyListWindow("Edit Logging Filter", "Log Text", Config.Logging.LogFilter);
            window.SetDarkMode(true);
            window.ShowDialog();
        }

        private void Button_ReloadDevices(object sender, RoutedEventArgs e)
        {
            if (Recognition.IsRecognizerRunning)
                Recognition.StopRecognizer();

            Devices.ForceReload();
        }
        private void Button_OpenDocs(object sender, RoutedEventArgs e)
            => UiHelper.StartProcess("https://github.com/PaciStardust/HOSCY");
        private void Button_OpenConfig(object sender, RoutedEventArgs e)
            => UiHelper.StartProcess(Config.ResourcePath);
    }
}
