using Hoscy.OscControl;
using Hoscy.Ui.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Hoscy.Ui.Pages
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
