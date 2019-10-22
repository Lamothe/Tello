using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Tello.UwpUI
{
    public class ListBoxLogger : Logger
    {
        private ListBox listBox;
        private ScrollViewer scroller;

        public ListBoxLogger(ListBox listBox, ScrollViewer scroller)
        {
            this.listBox = listBox;
            this.scroller = scroller;
        }

        private void WriteLineSync(string message)
        {
            listBox.Items.Add(message);
        }

        private async Task WriteLine(string message)
        {
            if (listBox.Dispatcher.HasThreadAccess)
            {
                WriteLineSync(message);
            }
            else
            {
                await listBox.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => WriteLineSync(message));
            }
        }

        public override async void WriteErrorLine(string message)
        {
            await WriteLine(message);
        }

        public override async void WriteInformationLine(string message)
        {
            await WriteLine(message);
        }

        public override async void WriteWarningLine(string message)
        {
            await WriteLine(message);
        }
    }
}
