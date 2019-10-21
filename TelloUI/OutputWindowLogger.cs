using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Tello;
using Xamarin.Essentials;

namespace TelloUI
{
    public class OutputWindowLogger : ILogger
    {
        private TextView outputWindow;

        private async Task WriteLine(string message)
        {
            await Task.Run(() => MainThread.BeginInvokeOnMainThread(() =>
            {
                outputWindow.Text += message + "\r\n";
            }));
        }

        public void SetOutputWindow(TextView outputWindow)
        {
            this.outputWindow = outputWindow;
        }

        public async Task WriteErrorLine(string message)
        {
            await WriteLine("E: " + message);
        }

        public async Task WriteInformationLine(string message)
        {
            await WriteLine("I: " + message);
        }

        public async Task WriteDebugLine(string message)
        {
            await WriteLine("D: " + message);
        }

        public async Task WriteWarningLine(string message)
        {
            await WriteLine("W: " + message);
        }
    }
}