using Hoscy.Services.Api;
using Hoscy.Services.OscControl;
using Hoscy.Services.Speech;
using Hoscy.Ui;
using Hoscy.Ui.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Hoscy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Version { get; private set; } = GetVersion();

        protected override void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            Logger.PInfo("HOSCY VERSION " + Version);
            Config.BackupFile(Utils.PathConfigFile);
            Osc.RecreateListener(); //This also loads the config
            Media.StartMediaDetection();

            if (Config.Debug.CheckUpdates)
                Updater.CheckForUpdates();

            Recognition.RecognitionChanged += PlayMuteSound;
        }

        private bool _currentListenStatus = false;
        private void PlayMuteSound(object? sender, RecognitionChangedEventArgs e)
        {
            if (_currentListenStatus != e.Listening && Config.Speech.PlayMuteSound && Running)
                SoundPlayer.Play(e.Listening ? SoundPlayer.Sound.Unmute : SoundPlayer.Sound.Mute);
            _currentListenStatus = e.Listening;
        }

        //Indicator for threads to stop
        public static bool Running { get; private set; } = true;
        protected override void OnExit(ExitEventArgs e)
        {
            Running = false;
            if (Recognition.IsRunning)
                Recognition.StopRecognizer();
            Config.SaveConfig();
        }

        //Error handling for unhandled errors
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Config.SaveConfig();
                Logger.Error(e.Exception, "A fatal error has occured, Hoscy will now shut down.");
            }
            catch { }
            Current.Shutdown(-1);
            Environment.Exit(-1);
        }

        #region Utility
        /// <summary>
        /// Creates a notification window
        /// </summary>
        /// <param name="title">Title of window</param>
        /// <param name="subtitle">Text above notification</param>
        /// <param name="notification">Contents of notification box</param>
        internal static void OpenNotificationWindow(string title, string subtitle, string notification, bool locking = false)
        {
            Current.Dispatcher.Invoke(() =>
            {
                var window = new NotificationWindow(title, subtitle, notification);
                window.SetDarkMode(true);
                if (locking)
                    window.ShowDialog();
                else
                    window.Show();
            });
        }

        /// <summary>
        /// Gets the current version from the assembly
        /// </summary>
        /// <returns></returns>
        private static string GetVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            return "v." + (assembly != null ? FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion : "???");
        }

        /// <summary>
        /// Gets the emedded ressource stream from the assembly by name
        /// </summary>
        /// <param name="name">Name of ressource</param>
        /// <returns>Stream of ressource</returns>
        internal static Stream? GetEmbeddedRessourceStream(string name)
            => Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        #endregion
    }
}
