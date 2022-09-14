using Hoscy.Ui.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy.Ui.Pages
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
