using System.Collections.Generic;
using System.Windows;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for DisplayChatHistoryWindow.xaml
    /// </summary>
    internal partial class DisplayChatHistoryWindow : Window
    {
        private static readonly List<string> _list = new();
        internal static DisplayChatHistoryWindow? Instance { get; private set; }

        public DisplayChatHistoryWindow()
        {
            InitializeComponent();
            Refresh();
            Instance = this;
        }

        private void Refresh()
        => listBox.Load(_list, _list.Count -1);

        internal void AddMessage(string message)
        {
            _list.Add(message);
            if (_list.Count > 50)
                _list.RemoveAt(0);
            Dispatcher.Invoke(() =>
            {
                Refresh();
            });
        }
    }
}
