using System.Windows;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    internal partial class NotificationWindow : Window
    {
        public NotificationWindow(string title, string subtitle, string notifiction)
        {
            InitializeComponent();

            Title = title;
            valueNotification.Text = notifiction;
            valueSubtitle.Text = subtitle;

            System.Media.SystemSounds.Hand.Play();
        }

        private void Button_OpenClipboard(object sender, RoutedEventArgs e)
            => Clipboard.SetText(valueNotification.Text);

        private void Button_OpenGithub(object sender, RoutedEventArgs e)
            => UiHelper.StartProcess(Utils.Github);
    }
}
