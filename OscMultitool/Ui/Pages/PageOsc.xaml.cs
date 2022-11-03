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
        private static bool unappliedOscChanges = false;

        public PageOsc()
        {
            InitializeComponent();
            CheckIndicators();
        }

        private void Button_ReloadListener(object sender, RoutedEventArgs e)
        {
            Osc.RecreateListener();
            unappliedOscChanges = false;
            CheckIndicators();
        }

        private void Button_ModifyRouting(object sender, RoutedEventArgs e)
        {
            var window = new ModifyOscRoutingFiltersWindow("Edit Routing Filters", Config.Osc.RoutingFilters);
            window.SetDarkMode(true);
            window.ShowDialog();
            Osc.RecreateListener();
            CheckIndicators();
        }

        private void CheckIndicators()
        {
            invalidFilterLabel.Visibility = Osc.HasInvalidFilters ? Visibility.Visible : Visibility.Hidden;
            changeIndicator.Visibility = unappliedOscChanges ? Visibility.Visible : Visibility.Hidden;
        }

        private void OscOscPortIn_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            unappliedOscChanges = Config.Osc.PortListen != Osc.ListenerPort;
            CheckIndicators();
        }

        private void AddressModified(object sender, System.Windows.Input.KeyEventArgs e)
        {
            unappliedOscChanges = true;
            CheckIndicators();
        }
    }
}
