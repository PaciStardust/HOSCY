using OscMultitool.Ui.Windows;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OscMultitool.Ui.Pages
{
    /// <summary>
    /// Interaction logic for PageDebug.xaml
    /// </summary>
    public partial class PageDebug : Page
    {
        public PageDebug()
        {
            InitializeComponent();
        }

        private void Button_OpenLogFilter(object sender, RoutedEventArgs e)
        {
            var window = new ModifyListWindow("Edit Logging Filter", Config.Logging.LogFilter);
            window.ShowDialog();
        }
    }
}
