using Hoscy.OscControl;
using Hoscy.Services.Api;
using Hoscy.Services.Speech;
using Hoscy.Ui;
using Hoscy.Ui.Controls;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.SetDarkMode(true);
            InitializeComponent();

            Osc.RecreateListener();

            listBox.SelectedIndex = 0;
            Media.StartMediaDetection();

            if (Config.Debug.CheckUpdates)
                HoscyClient.CheckForUpdates();
        }

        private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            int index = listBox.SelectedIndex;

            for(int i = 0; i < listBox.Items.Count; i++)
            {
                var navButton = (NavigationButton)listBox.Items[i];

                if (i != index)
                {
                    navButton.Background = UiHelper.ColorBack;
                    continue;
                }

                navButton.Background = UiHelper.ColorBackLight;
                navFrame.Navigate(navButton.NavPage);
                Application.Current.Resources["AccentColor"] = navButton.Color;
            }
        }
    }
}
