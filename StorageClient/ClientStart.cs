using StorageShared.Helpers;
using StorageShared.Models;
using System.Text;

namespace StorageClient
{
    internal class ClientStart
    {
        static async Task Main(string[] args)
        {
            while (true)
            {
                Logger logger = new Logger(true);

                StorageClient client = new StorageClient("localhost", 25566, logger: logger);

                bool connected = false;

                while (!connected)
                {
                    logger.Log("Connecting to server...");
                    connected = await client.StartAsync();

                    Console.WriteLine("Connected to server.\n");

                    if (!connected)
                    {
                        logger.Log("Failed to connect to server.");
                        await Task.Delay(1000);
                    }
                }

                while (true)
                {
                    await Task.Delay(100);

                    if (!client.Running)
                    {
                        logger.Log("Client is not running, exiting...\n");
                        break;
                    }
                }
            }
        }
    }
}
