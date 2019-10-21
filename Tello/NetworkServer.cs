using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Tello
{
    public class NetworkServer : IDisposable
    {
        private ILogger logger;
        private IPAddress address = IPAddress.Any;
        private int port;
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private Task task;

        public delegate void OnServerDataHandler(byte[] data);
        public event OnServerDataHandler OnServerData;

        public bool IsListening { get; set; }

        public NetworkServer(ILogger logger, int port)
        {
            this.logger = logger;
            this.port = port;
            socket.ReceiveTimeout = 5000;
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public byte[] Read()
        {
            var buffer = new byte[socket.ReceiveBufferSize];
            var bytesReceived = socket.Receive(buffer);
            return buffer.Take(bytesReceived).ToArray();
        }

        private IPEndPoint EndPoint => new IPEndPoint(address, port);

        public void Listen()
        {
            if (task == null)
            {
                task = Task.Run(async () =>
                {
                    IsListening = true;
                    while (IsListening)
                    {
                        try
                        {
                            OnServerData?.Invoke(Read());
                        }
                        catch (SocketException ex)
                        {
                            await logger.WriteErrorLine($"Server error: {ex.SocketErrorCode}");
                            System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            await logger.WriteErrorLine($"Server error: {ex.Message}");
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                });
            }
        }

        public async Task StopListening()
        {
            if (task != null)
            {
                await logger.WriteInformationLine("Stopping listener");
                IsListening = false;
                task.Wait();
                task = null;
                await logger.WriteInformationLine("Listener stopped");
            }
        }

        public void Dispose()
        {
            StopListening().Wait(2000);
        }
    }
}
