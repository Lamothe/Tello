using System;
using System.Threading.Tasks;
using Tello;

namespace TelloClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new ConsoleLogger();

            logger.WriteInformationLine("Connect to the Tello drone manually and press enter");
            Console.ReadKey();

            var commandClient = new CommandClient(logger);

            Task.Run(async () =>
            {
                await commandClient.Initialise();
                await commandClient.TakeOff();
                await commandClient.Left(20);
                await commandClient.Right(20);
                await commandClient.Land();
            });
        }
    }
}
