using Android.Widget;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Tello.AndroidUI
{
    public class OutputWindowLogger : Logger
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

        public override async void WriteErrorLine(string message)
        {
            await WriteLine("E: " + message);
        }

        public override async void WriteInformationLine(string message)
        {
            await WriteLine("I: " + message);
        }

        public override async void WriteDebugLine(string message)
        {
            await WriteLine("D: " + message);
        }

        public override async void WriteWarningLine(string message)
        {
            await WriteLine("W: " + message);
        }
    }
}