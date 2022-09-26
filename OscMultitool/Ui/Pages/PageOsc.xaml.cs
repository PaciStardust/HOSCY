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
            CheckInvalidIndicator();
        }

        private void Button_ReloadListener(object sender, RoutedEventArgs e)
        {
            Osc.RecreateListener();
            CheckInvalidIndicator();
        }

        private void Button_ModifyRouting(object sender, RoutedEventArgs e)
        {
            var window = new ModifyOscRoutingFiltersWindow("Modify Routing Filters", Config.Osc.RoutingFilters);
            window.SetDarkMode(true);
            window.ShowDialog();
            Osc.RecreateListener();
            CheckInvalidIndicator();
        }

        private void CheckInvalidIndicator()
            => invalidFilterLabel.Visibility = Osc.HasInvalidFilters ? Visibility.Visible : Visibility.Hidden;
    }
}
