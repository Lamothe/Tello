using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.WiFi;

namespace Tello.UwpUI
{
    internal class WiFi
    {
        private Logger logger = null;
        private bool connected = false;
        private WiFiAdapter wiFiAdapter = null;

        public delegate void OnConnectedHandler(object sender, string ssid);
        public event OnConnectedHandler OnConnected;

        public delegate void OnDisconnectedHandler(object sender);
        public event OnDisconnectedHandler OnDisconnected;

        private async Task<WiFiAdapter> GetWiFiAdapter()
        {
            if (wiFiAdapter == null)
            {
                var adapters = await WiFiAdapter.FindAllAdaptersAsync();
                wiFiAdapter = adapters.FirstOrDefault();
            }
            return wiFiAdapter;
        }

        public WiFi(Logger logger)
        {
            this.logger = logger;
        }

        public async Task RefreshNetworkConnection(List<WiFiAvailableNetwork> availableNetworks)
        {
            var availableNetwork = availableNetworks.FirstOrDefault(x => x.Ssid.StartsWith(Constants.WiFiSsid));
            if (availableNetwork == null)
            {
                if (connected)
                {
                    OnDisconnected?.Invoke(this);
                }
            }
            else
            {
                if (!connected)
                {
                    logger.WriteInformationLine($"Connecting to {availableNetwork.Ssid}");
                    await wiFiAdapter.ConnectAsync(availableNetwork, WiFiReconnectionKind.Manual);
                    OnConnected?.Invoke(this, availableNetwork.Ssid);
                }
            }
        }

        public async Task<List<WiFiAvailableNetwork>> Scan()
        {
            WiFiAdapter adapter = null;
            try
            {
                adapter = await GetWiFiAdapter();
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
