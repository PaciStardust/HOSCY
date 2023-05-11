using Hoscy.Services.Api;
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
    /// Interaction logic for PageDebug.xaml
    /// </summary>
    internal partial class PageConfig : Page //todo: [UI TWEAK] fix dropdown label inconsistency
    {
        public PageConfig()
        {
            InitializeComponent();
            versionText.Content = App.Version;
            LoadLoggingBox();
        }

        #region Logging Box
        private void LoadLoggingBox()
        {
            var loglevels = Enum.GetValues(typeof(LogSeverity)).Cast<LogSeverity>().ToList();
            if (loglevels == null)
            {
                Logger.Error("Failed to grab enum values for log level", false);
                return;
            }

            var logLevelIndex = loglevels.IndexOf(Config.Debug.MinimumLogSeverity);
            if (logLevelIndex == -1)
            {
                Logger.Error("Failed to grab logging level index corresponding to config value", false);
                return;
            }

            loggingLevelBox.Load(loglevels.Select(x => $"{(int)x} - {x}"), logLevelIndex == -1 ? 1 : logLevelIndex); 
        }

        private void LoggingLevelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var loglevels = Enum.GetValues(typeof(LogSeverity)).Cast<LogSeverity>().ToList();
            if (loglevels == null || loglevels.Count <= loggingLevelBox.SelectedIndex)
            {
                Logger.Error("Failed to assign selected logging level to config");
                return;
            }

            Config.Debug.MinimumLogSeverity = loglevels[loggingLevelBox.SelectedIndex];
        }
        #endregion

        #region Buttons
        private void Button_OpenLogFilter(object sender, RoutedEventArgs e)
        {
            var window = new ModifyFiltersWindow("Edit Logging Filter", Config.Debug.LogFilters);
            window.ShowDialogDark();
        }

        private void Button_ReloadDevices(object sender, RoutedEventArgs e)
        {
            if (Recognition.GetRunningStatus())
                Recognition.StopRecognizer();

            Devices.ForceReload();
        }
        private void Button_OpenDocs(object sender, RoutedEventArgs e)
            => Utils.StartProcess(Utils.Github);
        private void Button_OpenConfig(object sender, RoutedEventArgs e)
            => Utils.StartProcess(Utils.PathConfigFolder);
        private void Button_CheckUpdate(object sender, RoutedEventArgs e)
            => Updater.PerformUpdate();
        private void Button_SaveConfig(object sender, RoutedEventArgs e)
            => Config.SaveConfig();
        private void Button_ReloadMedia(object sender, RoutedEventArgs e)
            => Media.StartMediaDetection();
        #endregion
    }
}
