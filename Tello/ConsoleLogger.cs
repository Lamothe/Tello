using System;
using System.Threading.Tasks;

namespace Tello
{
    public class ConsoleLogger : Logger
    {
        public override void WriteDebugLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Error.WriteLineAsync(message);
        }

        public override void WriteErrorLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLineAsync(message);
        }

        public override void WriteInformationLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.WriteLineAsync(message);
        }

        public override void WriteWarningLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLineAsync(message);
        }
    }
}
