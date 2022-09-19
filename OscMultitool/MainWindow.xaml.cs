using Hoscy.OscControl;
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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //Init
            Osc.RecreateListener();

            InitializeComponent();
            listBox.SelectedIndex = 0;
            Media.StartMediaDetection();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (NavigationButton)listBox.Items[listBox.SelectedIndex];
            navFrame.Navigate(item.NavPage);
            navBorder.BorderBrush = item.Color;
        }
    }
}
