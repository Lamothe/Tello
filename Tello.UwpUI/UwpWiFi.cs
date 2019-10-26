using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.WiFi;

namespace Tello.UwpUI
{
    internal class UwpWiFi
    {
        protected Logger logger = null;

        public delegate void OnConnectedHandler(object sender, string ssid);
        public event OnConnectedHandler OnConnected;

        public delegate void OnDisconnectedHandler(object sender);
        public event OnDisconnectedHandler OnDisconnected;

        public bool IsConnected { get; protected set; } = false;

        private WiFiAdapter wiFiAdapter = null;

        private async Task<WiFiAdapter> GetAdapter()
        {
            if (wiFiAdapter == null)
            {
                var adapters = await WiFiAdapter.FindAllAdaptersAsync();
                wiFiAdapter = adapters.FirstOrDefault();
            }
            return wiFiAdapter;
        }

        public UwpWiFi(Logger logger)
        {
            this.logger = logger;
        }

        public async Task RefreshNetworkConnection(List<WiFiAvailableNetwork> availableNetworks)
        {
            var availableNetwork = availableNetworks.FirstOrDefault(x => x.Ssid.StartsWith(Constants.WiFiSsid));
            if (availableNetwork == null)
            {
                if (IsConnected)
                {
                    IsConnected = false;
                    OnDisconnected?.Invoke(this);
                }
            }
            else
            {
                if (!IsConnected)
                {
                    logger.WriteInformationLine($"Connecting to {availableNetwork.Ssid}");
                    await wiFiAdapter.ConnectAsync(availableNetwork, WiFiReconnectionKind.Manual);
                    IsConnected = true;
                    OnConnected?.Invoke(this, availableNetwork.Ssid);
                }
            }
        }

        public async Task<List<WiFiAvailableNetwork>> Scan()
        {
            WiFiAdapter adapter = null;
            try
            {
                adapter = await GetAdapter();
                await adapter.ScanAsync();
            }
            catch (System.Threading.ThreadAbortException)
            {
                logger.WriteInformationLine("Network scan request aborted.");
            }

            var availableNetworks = adapter.NetworkReport.AvailableNetworks
                .Where(x => !string.IsNullOrWhiteSpace(x.Ssid))
                .OrderByDescending(x => x.SignalBars)
                .Take(4)
                .ToList();
            return availableNetworks;
        }
    }
}
