using System;
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
        private static event Action ListAdded = delegate { };

        public DisplayChatHistoryWindow()
        {
            InitializeComponent();
            Refresh();
            ListAdded += UpdateList;
        }

        private void Refresh()
        => listBox.Load(_list, _list.Count -1);

        internal static void AddMessage(string message)
        {
            _list.Add(message);
            if (_list.Count > 50)
                _list.RemoveAt(0);
            ListAdded.Invoke();
        }

        private void UpdateList()
        {
            Dispatcher.Invoke(() =>
            {
                Refresh();
            });
        }
    }
}
