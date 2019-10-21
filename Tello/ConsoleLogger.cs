using System;
using System.Threading.Tasks;

namespace Tello
{
    public class ConsoleLogger : ILogger
    {
        public async Task WriteDebugLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            await Console.Error.WriteLineAsync(message);
        }

        public async Task WriteErrorLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            await Console.Error.WriteLineAsync(message);
        }

        public async Task WriteInformationLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            await Console.Error.WriteLineAsync(message);
        }

        public async Task WriteWarningLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            await Console.Error.WriteLineAsync(message);
        }
    }
}
