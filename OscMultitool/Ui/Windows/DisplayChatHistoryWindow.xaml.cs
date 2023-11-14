using Hoscy.Services.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hoscy.Ui.Windows
{
    /// <summary>
    /// Interaction logic for DisplayChatHistoryWindow.xaml
    /// </summary>
    internal partial class DisplayChatHistoryWindow : Window
    {
        private static readonly List<TextProcessorResult> _list = new();
        private static event Action ListAdded = delegate { };
        private bool _selectionInputsToBox = false;

        public DisplayChatHistoryWindow()
        {
            InitializeComponent();
            Refresh();
            ListAdded += UpdateList;
            _selectionInputsToBox = true;
        }

        private void Refresh()
        {
            _selectionInputsToBox = false;
            var index = _list.Count - 1;
            listBox.Load(_list.Select(x => $"[{x.InputSource.ToString()[0]}] {x.Message}"), index);
            if (index > -1)
                listBox.ScrollIntoView(listBox.Items[index]);
            _selectionInputsToBox = true;
        }

        internal static void AddMessage(TextProcessorResult result)
        {
            _list.Add(result);
            if (_list.Count > 50)
                _list.RemoveAt(0);
            ListAdded.Invoke();
        }

        private void UpdateList()
            => Dispatcher.Invoke(Refresh);

        private void TextBox_KeyPressed(object sender, KeyEventArgs e)
            => ManualInputHelper.KeyPress(textBox, e);

        private void Button_Send(object sender, RoutedEventArgs e)
            => ManualInputHelper.SendMessage(textBox);

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = listBox.SelectedIndex;
            if (!_selectionInputsToBox || index < 0 || index >= _list.Count)
                return;
            textBox.Text = _list[index].Message;
        }
    }
}
