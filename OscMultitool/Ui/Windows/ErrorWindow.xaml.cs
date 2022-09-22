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
using System.Windows.Shapes;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public ErrorWindow(string title, string error)
        {
            InitializeComponent();

            Title = title;
            valueError.Text = error;
        }

        private void Button_OpenClipboard(object sender, RoutedEventArgs e)
            => Clipboard.SetText(valueError.Text);

        private void Button_OpenGithub(object sender, RoutedEventArgs e)
            => UiHelper.StartProcess("https://github.com/PaciStardust/HOSCY");
    }
}
