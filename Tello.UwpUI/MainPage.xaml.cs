using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Tello.UwpUI
{
    public sealed partial class MainPage : Page
    {
        private ListBoxLogger logger = null;
        private TelloService telloService = null;

        public MainPage()
        {
            this.InitializeComponent();

            logger = new ListBoxLogger(Output, OutputScroller);

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Application.Current.UnhandledException += Current_UnhandledException;

            Initialise();
        }

        private async void Initialise()
        {
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                logger.WriteErrorLine("You are not allowed to access the Wi-Fi device.");
            }
            else
            {
                telloService = new TelloService(logger);
                telloService.Start();
                telloService.OnInitialised += TelloService_OnInitialised;
                telloService.OnAvailableNetworksUpdated += TelloService_OnAvailableNetworksUpdated;

                System.Timers.Timer timer = new System.Timers.Timer(1000);
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }
        }

        private async void TelloService_OnAvailableNetworksUpdated(object sender, List<WiFiAvailableNetwork> availableNetworks)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                AvailableNetworks.Items.Clear();
                availableNetworks.ForEach(x => AvailableNetworks.Items.Add(x.Ssid));
            });
        }

        private async void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                OutputScroller.ChangeView(0, double.MaxValue, 1);
                UpdateState();
            });
        }

        private void TelloService_OnInitialised(object sender)
        {
            Video.SetMediaStreamSource((sender as TelloService).MediaStreamSource);
            Video.Play();
        }

        private void UpdateState()
        {
            var fields = telloService.State;
            var status = $"{DateTime.Now.ToString()} - {(telloService.IsDroneConnected ? "Connected" : "Not connected")}\r\n";
            foreach (var key in fields.Keys)
            {
                status += $"{key}: {fields[key]}\r\n";
            }

            Task.Run(async () =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Status.Text = status);
            });
        }

        private void Current_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            logger.WriteErrorLine($"Unhandled Exception: {e.Exception.Message}");
        }

        private async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            logger.WriteDebugLine($"Key pressed {args.VirtualKey}");

            if (!telloService.IsDroneConnected)
            {
                logger.WriteInformationLine("Not connected");
            }
            else
            {
                switch (args.VirtualKey)
                {
                    case Windows.System.VirtualKey.Subtract: await telloService.TakeOff(); break;
                    case Windows.System.VirtualKey.Add: await telloService.Land(); break;

                    case Windows.System.VirtualKey.Left: await telloService.RotateLeft(45); break;
                    case Windows.System.VirtualKey.Right: await telloService.RotateRight(45); break;

                    case Windows.System.VirtualKey.Home: await telloService.Forward(50); break;
                    case Windows.System.VirtualKey.Up: await telloService.SetSpeed(20); break;
                    case Windows.System.VirtualKey.Clear: await telloService.Stop(); break;

                    case Windows.System.VirtualKey.Divide: await telloService.RotateLeft(180); break;
                    case Windows.System.VirtualKey.Multiply: await telloService.RotateLeft(180); break;

                    case Windows.System.VirtualKey.PageUp: await telloService.Up(20); break;
                    case Windows.System.VirtualKey.PageDown: await telloService.Down(20); break;

                    case Windows.System.VirtualKey.Down: await telloService.FlipBackward(); break;

                    case Windows.System.VirtualKey.Enter: await telloService.Emergency(); break;

                    case Windows.System.VirtualKey.A: await telloService.GetSerialNumber(); break;
                    case Windows.System.VirtualKey.S: await telloService.GetSpeed(); break;
                }
            }
        }
    }
}
