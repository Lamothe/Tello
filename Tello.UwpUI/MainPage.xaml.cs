using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Tello.UwpUI
{
    public sealed partial class MainPage : Page
    {
        private ListBoxLogger logger = null;
        private bool running { get; set; } = false;
        private CommandClient commandClient { get; set; } = null;
        private StateServer stateClient { get; set; } = null;
        private VideoServer videoServer { get; set; } = null;
        private WiFi wiFi { get; set; } = null;
        private bool connected = false;

        public MainPage()
        {
            this.InitializeComponent();

            logger = new ListBoxLogger(Output, OutputScroller);

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Application.Current.UnhandledException += Current_UnhandledException;

            commandClient = new CommandClient(logger);
            stateClient = new StateServer(logger);
            videoServer = new VideoServer(logger);
            wiFi = new WiFi(logger);

            stateClient.OnStateUpdate += StateClient_OnStateUpdate;
            wiFi.OnConnected += WiFi_OnConnected;
            wiFi.OnDisconnected += WiFi_OnDisconnected;

            Task.Run(async () =>
            {
                await Initialise();
                await Loop();
            });
        }

        private async void WiFi_OnConnected(object sender, string ssid)
        {
            connected = true;
            await commandClient.Initialise();
            await commandClient.EnableVideo();
            stateClient.Listen();
            videoServer.Listen();
            logger.WriteInformationLine($"Connected to {ssid}");
        }

        private async void WiFi_OnDisconnected(object sender)
        {
            connected = false;
            logger.WriteErrorLine($"Disconnected from Tello");
            await commandClient.DisableVideo();
            videoServer.StopListening();
            stateClient.StopListening();
        }

        private void UpdateState()
        {
            lock (stateClient)
            {
                var fields = stateClient.State;
                var status = $"{DateTime.Now.ToString()} - {(connected ? "Connected" : "Not connected")}\r\n";
                foreach (var key in fields.Keys)
                {
                    status += $"{key}: {fields[key]}\r\n";
                }

                Task.Run(async () =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => Status.Text = status);
                });
            }
        }

        private void StateClient_OnStateUpdate()
        {
            UpdateState();
        }

        private void Current_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            logger.WriteErrorLine($"Unhandled Exception: {e.Exception.Message}");
        }

        private async Task Initialise()
        {
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                logger.WriteErrorLine("You are not allowed to access the Wi-Fi device.");
            }
            else
            {
                var wifi = new WiFi(logger);
                var videoEncodingProperties = VideoEncodingProperties.CreateH264();
                videoEncodingProperties.Height = 720;
                videoEncodingProperties.Width = 960;

                var mediaStreamSource = new MediaStreamSource(new VideoStreamDescriptor(videoEncodingProperties));
                mediaStreamSource.BufferTime = TimeSpan.FromSeconds(0);
                mediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Video.SetMediaStreamSource(mediaStreamSource);
                    Video.Play();
                });
            }
        }

        private async Task Loop()
        {
            logger.WriteInformationLine($"Waiting for WiFi connection");

            running = true;
            while (running)
            {
                try
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () => OutputScroller.ChangeView(0, double.MaxValue, 1));
                    var availableNetworks = await wiFi.Scan();
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        AvailableNetworks.Items.Clear();
                        availableNetworks.ForEach(x => AvailableNetworks.Items.Add(x.Ssid));
                    });
                    await wiFi.RefreshNetworkConnection(availableNetworks);
                    UpdateState();
                }
                catch (Exception ex)
                {
                    logger.WriteErrorLine($"Loop Exception: {ex.Message}");
                }

                await Task.Delay(1000);
            }
        }

        private async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            logger.WriteDebugLine($"Key pressed {args.VirtualKey}");

            if (!connected)
            {
                logger.WriteInformationLine("Not connected");
            }
            else
            {
                switch (args.VirtualKey)
                {
                    case Windows.System.VirtualKey.Space: await commandClient.Initialise(); break;
                    case Windows.System.VirtualKey.Subtract: await commandClient.TakeOff(); break;
                    case Windows.System.VirtualKey.Add: await commandClient.Land(); break;
                    case Windows.System.VirtualKey.Left: await commandClient.RotateLeft(30); break;
                    case Windows.System.VirtualKey.Right: await commandClient.RotateRight(30); break;
                    case Windows.System.VirtualKey.Up: await commandClient.Forward(50); break;
                    case Windows.System.VirtualKey.Clear: await commandClient.FlipForward(); break;
                    case Windows.System.VirtualKey.Down: await commandClient.FlipBackward(); break;
                }
            }
        }

        private void MediaStreamSource_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            var sample = videoServer.GetSample();
            if (sample != null)
            {
                args.Request.Sample = MediaStreamSample.CreateFromBuffer(sample.Buffer.AsBuffer(), sample.TimeIndex);
                args.Request.Sample.Duration = sample.Duration;
            }
        }
    }
}
