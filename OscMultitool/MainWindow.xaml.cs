using Hoscy.Services.Api;
using Hoscy.Ui;
using Hoscy.Ui.Controls;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        private bool _firstLoad = true;

        public MainWindow()
        {
            this.SetDarkMode(true);
            InitializeComponent();

            listBox.SelectedIndex = 0;
            Hotkeys.Register();
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

                if (!_firstLoad)
                    Config.SaveConfig();
                else
                    _firstLoad = false;

                navButton.Background = UiHelper.ColorBackLight;
                navFrame.Navigate(navButton.NavPage);
                Application.Current.Resources["AccentColor"] = navButton.Color;
            }
        }
    }
}
