using NAudio.Wave;
using OscMultitool.OscControl;
using OscMultitool.Services;
using OscMultitool.Services.Speech;
using OscMultitool.Ui.Controls;
using OscMultitool.Ui.Pages;
using OscMultitool.Ui.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OscMultitool
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
