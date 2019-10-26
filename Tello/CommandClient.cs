using System;
using System.Threading.Tasks;

namespace Tello
{
    public class CommandClient : NetworkClient
    {
        private Logger logger = null;

        public delegate void OnCommandSendingHandler(object sender, string command);
        public event OnCommandSendingHandler OnCommandSending;

        public delegate void OnCommandResponseHandler(object sender, string command, string response);
        public event OnCommandResponseHandler OnCommandResponse;

        public CommandClient(Logger logger)
            : base(logger, "192.168.10.1", 8889)
        {
            this.logger = logger;
        }

        public async Task<string> Send(string command)
        {
            OnCommandSending?.Invoke(this, command);
            await Write(command);
            var response = await Read();
            OnCommandResponse?.Invoke(this, command, response);
            return response;
        }

        public async Task<bool> SendSet(string command)
        {
            var response = await Send(command);

            if (response != "ok")
            {
                logger.WriteErrorLine($"Invalid response from drone '{response}'");
                return false;
            }

            return true;
        }

        public async Task<int?> SendGetInt(string command)
        {
            return int.TryParse(await Send(command), out int value) ? value : (int?)null;
        }

        public async Task<bool> Initialise()
        {
            return await SendSet("command");
        }

        public async Task<bool> Emergency() => await SendSet("emergency");

        public async Task<string> GetSerialNumber() => await Send("sn?");

        public async Task<int?> GetSpeed() => await SendGetInt("speed?");

        public async Task<bool> SetSpeed(int speed) => await SendSet($"speed {speed}");

        public async Task<bool> Stop() => await SendSet("stop");

        public async Task<bool> TakeOff() => await SendSet("takeoff");

        public async Task<bool> Land() => await SendSet("land");

        public async Task<bool> Up(int cm) => await SendSet($"up {cm}");

        public async Task<bool> Down(int cm) => await SendSet($"down {cm}");

        public async Task<bool> Left(int cm) => await SendSet($"left {cm}");

        public async Task<bool> Right(int cm) => await SendSet($"right {cm}");

        public async Task<bool> Forward(int cm) => await SendSet($"forward {cm}");

        public async Task<bool> Backward(int cm) => await SendSet($"back {cm}");

        public async Task<bool> EnableVideo() => await SendSet("streamon");

        public async Task<bool> DisableVideo() => await SendSet("streamoff");

        public async Task<bool> FlipBackward() => await SendSet("flip b");

        public async Task<bool> FlipForward() => await SendSet("flip f");

        public async Task<bool> RotateLeft(int degrees) => await SendSet($"ccw {degrees}");

        public async Task<bool> RotateRight(int degrees) => await SendSet($"cw {degrees}");
    }
}
