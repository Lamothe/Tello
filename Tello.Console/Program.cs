using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Tello;

namespace TelloClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new ConsoleLogger();

            var serviceProvider = new ServiceCollection()
                .AddSingleton<Logger>(logger)
                .BuildServiceProvider();

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
