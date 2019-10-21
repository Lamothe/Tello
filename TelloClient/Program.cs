using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace TelloClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<Tello.ICommandClient, Tello.CommandClient>()
                .AddSingleton<Tello.ILogger, Tello.ConsoleLogger>()
                .BuildServiceProvider();

            var commandClient = serviceProvider.GetService<Tello.ICommandClient>();

            while (!commandClient.Initialise())
            {
                System.Threading.Thread.Sleep(2000);
            }

            commandClient.TakeOff();
            commandClient.Left(20);
            commandClient.Right(20);
            commandClient.Land();
        }
    }
}
