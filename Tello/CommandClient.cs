using System.Threading.Tasks;

namespace Tello
{
    public class CommandClient : NetworkClient
    {
        private bool connected { get; set; } = false;

        public CommandClient(ILogger logger)
            : base(logger, "192.168.10.1", 8889)
        {
        }

        public async Task<bool> Send(string command)
        {
            await Write(command);
            return await Read() == "ok";
        }

        public async Task<bool> Initialise()
        {
            if (!connected)
            {
                Connect();
                connected = true;
            }

            return await Send("command");
        }

        public async Task<bool> TakeOff() => await Send("takeoff");

        public async Task<bool> Land() => await Send("land");

        public async Task<bool> Up(int cm) => await Send($"up {cm}");

        public async Task<bool> Down(int cm) => await Send($"down {cm}");

        public async Task<bool> Left(int cm) => await Send($"left {cm}");

        public async Task<bool> Right(int cm) => await Send($"right {cm}");

        public async Task<bool> Forward(int cm) => await Send($"forward {cm}");

        public async Task<bool> Backward(int cm) => await Send($"back {cm}");

        public async Task<bool> EnableVideo() => await Send("streamon");

        public async Task<bool> DisableVideo() => await Send("streamoff");

        public async Task<bool> FlipBackward() => await Send("flip b");

        public async Task<bool> FlipForward() => await Send("flip f");

        public async Task<bool> RotateLeft(int degrees) => await Send($"ccw {degrees}");

        public async Task<bool> RotateRight(int degrees) => await Send($"cw {degrees}");
    }
}
