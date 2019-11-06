using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Media.Core;
using Windows.Media.MediaProperties;

namespace Tello.UwpUI
{
    public class TelloService : CommandClient
    {
        private Logger logger;
        private bool running { get; set; } = false;
        private StateServer stateClient { get; set; } = null;
        private VideoServer videoServer { get; set; } = null;
        private UwpWiFi wiFi { get; set; } = null;
        private ConcurrentDictionary<string, string> state { get; } = new ConcurrentDictionary<string, string>();
        public bool IsDroneConnected { get; private set; } = false;
        public bool IsWiFiConnected { get; private set; } = false;

        public Dictionary<string, string> State => state.ToDictionary(x => x.Key, x => x.Value);

        public delegate void OnInitialisedHandler(object sender);
        public event OnInitialisedHandler OnInitialised;

        public delegate void OnDisconnectedHandler(object sender);
        public event OnDisconnectedHandler OnDisconnected;

        public delegate void OnAvailableNetworksUpdatedHandler(object sender, List<WiFiAvailableNetwork> availableNetworks);
        public event OnAvailableNetworksUpdatedHandler OnAvailableNetworksUpdated;

        public MediaStreamSource MediaStreamSource { get; private set; }

        public TelloService(Logger logger)
            : base(logger)
        {
            this.logger = logger;
        }

        public async void Start()
        {
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                logger.WriteErrorLine("You are not allowed to access the Wi-Fi device.");
            }
            else
            {
                stateClient = new StateServer(logger);
                videoServer = new VideoServer(logger);
                wiFi = new UwpWiFi(logger);

                stateClient.OnStateUpdate += (state) =>
                {
                    state.Keys.ToList().ForEach(x => this.state[x] = state[x]);
                };

                wiFi.OnConnected += WiFi_OnConnected;
                wiFi.OnDisconnected += WiFi_OnDisconnected;

                var videoEncodingProperties = VideoEncodingProperties.CreateH264();
                videoEncodingProperties.Height = 720;
                videoEncodingProperties.Width = 960;

                MediaStreamSource = new MediaStreamSource(new VideoStreamDescriptor(videoEncodingProperties));
                MediaStreamSource.BufferTime = TimeSpan.FromSeconds(0);
                MediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;

                Loop();

                OnInitialised?.Invoke(this);
            }
        }

        private void Loop()
        {
            logger.WriteInformationLine($"Waiting for WiFi connection");

            Task.Run(async () =>
            {
                running = true;
                while (running)
                {
                    try
                    {
                        var availableNetworks = await wiFi.Scan();
                        OnAvailableNetworksUpdated?.Invoke(this, availableNetworks);
                        await wiFi.RefreshNetworkConnection(availableNetworks);
                    }
                    catch (Exception ex)
                    {
                        logger.WriteErrorLine($"Loop Exception: {ex.Message}");
                    }

                    await Task.Delay(1000);
                }
            });
        }

        private async void WiFi_OnConnected(object sender, string ssid)
        {
            IsWiFiConnected = true;
            if (Initialise().Result)
            {
                IsDroneConnected = true;
                logger.WriteInformationLine($"Serial Number: {await GetSerialNumber()}");
                await EnableVideo();
                stateClient.Listen();
                videoServer.Listen();
                logger.WriteInformationLine($"Connected to {ssid}");
            }
        }

        private async void WiFi_OnDisconnected(object sender)
        {
            IsDroneConnected = false;
            IsWiFiConnected = false;
            logger.WriteErrorLine($"Disconnected from Tello");
            await DisableVideo();
            videoServer.StopListening();
            stateClient.StopListening();
            OnDisconnected?.Invoke(this);
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
