using OscMultitool.OscControl;
using OscMultitool.Services.Speech;
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
    /// Interaction logic for PageOsc.xaml
    /// </summary>
    public partial class PageOsc : Page
    {
        public PageOsc()
        {
            InitializeComponent();
        }

        private void Button_ReloadListener(object sender, RoutedEventArgs e)
            => Osc.RecreateListener();

        private void Button_ModifyRouting(object sender, RoutedEventArgs e)
        {
            var window = new ModifyOscRoutingFiltersWindow("Modify Routing Filters", Config.Osc.RoutingFilters);
            window.ShowDialog();
            Osc.RecreateListener();
        }
    }
}
