using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Tello
{
    public class NetworkClient : IDisposable
    {
        private UdpClient udpClient = new UdpClient();
        private Logger logger;
        private string address;
        private int port;
        private bool connected = false;

        public int TimeoutMilliseconds { get; set; } = 1000;

        public NetworkClient(Logger logger, string address, int port)
        {
            this.logger = logger;
            this.address = address;
            this.port = port;
        }

        public void Dispose()
        {
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient.Dispose();
            }
        }

        public async Task<string> Read()
        {
            try
            {
                logger.WriteDebugLine("Waiting for response ...");
                var asyncResult = udpClient.ReceiveAsync();
                if (!asyncResult.Wait(TimeoutMilliseconds))
                {
                    logger.WriteDebugLine("Timeout waiting for response");
                    return null;
                }

                var result = await asyncResult;
                var buffer = result.Buffer;
                var text = Encoding.UTF8.GetString(buffer);
                logger.WriteDebugLine($"Recieved '{text}' ({buffer} bytes) from '{address}:{port}'");
                return text;
            }
            catch (Exception ex)
            {
                logger.WriteErrorLine($"Read error: {ex.Message}");
            }

            return null;
        }

        private IPEndPoint EndPoint => new IPEndPoint(IPAddress.Parse(address), port);

        public void Bind()
        {
            Bind(EndPoint);
        }

        public void Bind(IPEndPoint endPoint)
        {
            logger.WriteInformationLine($"Binding to {endPoint.Address}:{endPoint.Port}");
            udpClient.Client.Bind(endPoint);
        }

        public void Connect()
        {
            logger.WriteInformationLine($"Connecting to {address}:{port}");
            udpClient.Connect(EndPoint);
            connected = true;
        }

        public async Task Write(string message)
        {
            try
            {
                var datagram = Encoding.UTF8.GetBytes(message);
                logger.WriteDebugLine($"Sending '{message}' to '{address}:{port}' ...");
                var bytesSent = connected
                    ? await udpClient.SendAsync(datagram, datagram.Length)
                    : await udpClient.SendAsync(datagram, datagram.Length, EndPoint);
                logger.WriteDebugLine($"Sent {bytesSent} bytes.");
            }
            catch (Exception ex)
            {
                logger.WriteErrorLine($"Write error: {ex.Message}");
            }
        }
    }
}
