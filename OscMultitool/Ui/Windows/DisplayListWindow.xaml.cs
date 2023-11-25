using System.Collections.Generic;
using System.Windows;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for DisplayListWindow.xaml
    /// </summary>
    internal partial class DisplayListWindow : Window
    {
        private readonly List<string> _list;

        public DisplayListWindow(string title, List<string> list)
        {
            InitializeComponent();

            _list = list;
            Title = title;
            listBox.Load(_list, -1);
        }
    }
}
