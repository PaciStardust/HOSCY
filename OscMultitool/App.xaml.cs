using Hoscy.Services.Speech;
using Hoscy.Ui;
using Hoscy.Ui.Windows;
using System;
using System.Windows;

namespace Hoscy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //Indicator for threads to stop
        public static bool Running { get; private set; } = true;
        protected override void OnExit(ExitEventArgs e)
        {
            Running = false;
            if (Recognition.IsRecognizerRunning)
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
        public static void OpenNotificationWindow(string title, string subtitle, string notification, bool locking = false)
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
        #endregion
    }
}
