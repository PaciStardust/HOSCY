using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
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
            => UiHelper.StartProcess(Config.Github);
    }
}
