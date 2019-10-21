using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tello;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace TelloUwpUI
{
    public class ListBoxLogger : ILogger
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

        public async Task WriteDebugLine(string message)
        {
            // await WriteLine(message);
        }

        public async Task WriteErrorLine(string message)
        {
            await WriteLine(message);
        }

        public async Task WriteInformationLine(string message)
        {
            await WriteLine(message);
        }

        public async Task WriteWarningLine(string message)
        {
            await WriteLine(message);
        }
    }
}
