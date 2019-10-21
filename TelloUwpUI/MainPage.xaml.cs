using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Tello;
using Windows.Devices.Radios;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace TelloUwpUI
{
    public sealed partial class MainPage : Page
    {
        private ListBoxLogger logger = null;
        private bool connected { get; set; } = false;
        private bool running { get; set; } = false;
        private WiFiAdapter wiFiAdapter { get; set; } = null;
        private CommandClient commandClient { get; set; } = null;
        private StateServer stateClient { get; set; } = null;
        private VideoServer videoServer { get; set; } = null;

        private readonly ConcurrentQueue<VideoSample> samples = new ConcurrentQueue<VideoSample>();
        private Stopwatch timer = new Stopwatch();
        private TimeSpan elapsed = TimeSpan.FromSeconds(0);

        public MainPage()
        {
            this.InitializeComponent();

            logger = new ListBoxLogger(Output, OutputScroller);

            new ServiceCollection()
                .AddSingleton<Tello.ILogger>(logger)
                .BuildServiceProvider();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Application.Current.UnhandledException += Current_UnhandledException;

            commandClient = new CommandClient(logger);
            stateClient = new StateServer(logger);
            videoServer = new VideoServer(logger);

            stateClient.OnStateUpdate += StateClient_OnStateUpdate;
            videoServer.OnServerData += VideoServer_OnServerData;

            Task.Run(async () =>
            {
                await Initialise();
                await Loop();
            });
        }

        private void VideoServer_OnServerData(byte[] data)
        {
            var sample = new VideoSample(data, elapsed, timer.Elapsed - elapsed);
            elapsed = timer.Elapsed;
            
            if (!timer.IsRunning)
            {
                timer.Start();
            }

            while (samples.Count > 1000)
            {
                samples.TryDequeue(out VideoSample _);
            }

            samples.Enqueue(sample);
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
            logger.WriteErrorLine($"Unhandled Exception: {e.Exception.Message}").Wait();
        }

        private async Task Initialise()
        {
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                await logger.WriteErrorLine("You are not allowed to access the Wi-Fi device.");
            }
            else
            {
                var adapters = await WiFiAdapter.FindAllAdaptersAsync();
                wiFiAdapter = adapters.FirstOrDefault();
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
            await logger.WriteInformationLine($"Waiting for connection to {Tello.Constants.WiFiSsid}");

            running = true;
            while (running)
            {
                try
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () => OutputScroller.ChangeView(0, double.MaxValue, 1));
                    await RefreshNetworkConnection(Tello.Constants.WiFiSsid);
                    UpdateState();
                }
                catch (Exception ex)
                {
                    await logger.WriteErrorLine($"Loop Exception: {ex.Message}");
                }

                await Task.Delay(1000);
            }
        }

        private async Task<List<WiFiAvailableNetwork>> Scan()
        {
            try
            {
                await wiFiAdapter.ScanAsync();
            }
            catch (System.Threading.ThreadAbortException)
            {
                await logger.WriteInformationLine("Network scan request aborted.");
            }

            var availableNetworks = wiFiAdapter.NetworkReport.AvailableNetworks
                .Where(x => !string.IsNullOrWhiteSpace(x.Ssid))
                .OrderByDescending(x => x.SignalBars)
                .Take(4)
                .ToList();

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AvailableNetworks.Items.Clear();
                availableNetworks.ForEach(x => AvailableNetworks.Items.Add(x.Ssid));
            });

            return availableNetworks;
        }

        private async Task RefreshNetworkConnection(string ssid)
        {
            var availableNetworks = await Scan();

            var availableNetwork = availableNetworks.FirstOrDefault(x => x.Ssid == ssid);
            if (availableNetwork == null)
            {
                if (connected)
                {
                    await OnDisconnect(ssid);
                }
            }
            else
            {
                if (!connected)
                {
                    await logger.WriteInformationLine($"Connecting to {availableNetwork.Ssid}");
                    await wiFiAdapter.ConnectAsync(availableNetwork, WiFiReconnectionKind.Manual);
                    await OnConnect(availableNetwork.Ssid);
                }
            }
        }

        private async Task OnConnect(string ssid)
        {
            if (!connected)
            {
                await commandClient.Initialise();
                await commandClient.EnableVideo();
                stateClient.Listen();
                videoServer.Listen();
                await logger.WriteInformationLine($"Connected to {ssid}");
                connected = true;
            }
        }

        private async Task OnDisconnect(string ssid)
        {
            if (connected)
            {
                await logger.WriteErrorLine($"Disconnected from {ssid}");
                await commandClient.DisableVideo();
                await videoServer.StopListening();
                await stateClient.StopListening();
                connected = false;
            }
        }

        private async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            await logger.WriteDebugLine($"Key pressed {args.VirtualKey}");

            if (!connected)
            {
                await logger.WriteInformationLine("Not connected");
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
            var wait = new SpinWait();
            VideoSample sample;
            while (samples.Count == 0 || !samples.TryDequeue(out sample))
            {
                wait.SpinOnce();
            }

            if (sample != null)
            {
                args.Request.Sample = MediaStreamSample.CreateFromBuffer(sample.Buffer.AsBuffer(), sample.TimeIndex);
                args.Request.Sample.Duration = sample.Duration;
            }
        }

        public sealed class VideoSample
        {
            public byte[] Buffer { get; }
            public TimeSpan TimeIndex { get; }
            public TimeSpan Duration { get; }
            public int Length => Buffer.Length;

            public VideoSample(byte[] buffer, TimeSpan timeIndex, TimeSpan duration)
            {
                Buffer = buffer;
                TimeIndex = timeIndex;
                Duration = duration;
            }
        }
    }
}
