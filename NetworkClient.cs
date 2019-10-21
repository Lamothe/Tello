public class NetworkClient
{
    private Socket socket;
    private ILogger logger;
    private readonly object lockObject = new object();
    
    public NetworkClient(ILogger logger)
    {
        this.logger = logger;
    }

    private void Send(string message)
    {
        try
        {
            var data = Encoding.ASCII.GetBytes(message);
            EnsureSocket();
            socket?.SendTo(data, endPoint);
        }
        catch (Exception ex)
        {
            _logger.WriteLine($"An error occurred while sending the metrics: {ex.Message}");
            ResetUdpClient();
        }
    }   

    private void EnsureSocket()
    {
        if (socket != null)
        {
            return;
        }

        lock (lockObject)
        {
            if (socket == null)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
        }
    }

    private void ResetUdpClient()
    {
        if (socket == null)
        {
            return;
        }

        lock (lockObject)
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                socket.Close();
            }
            catch
            {
                logger.WriteLine("An error occurred while calling Close() on the socket.");
            }
            finally
            {
                socket = null;
            }
        }
    }

    public void Dispose()
    {
        ResetUdpClient();
    }
}
