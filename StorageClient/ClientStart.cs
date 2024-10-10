using StorageShared.Helpers;
using StorageShared.Models;

namespace StorageClient
{
    internal class ClientStart
    {
        static async Task Main(string[] args)
        {
            while (true)
            {
                Logger logger = new Logger(true);

                ClientArguments? arguments = ClientArguments.ReadFromRuntimeArguments(args, logger);
                if (arguments == null)
                {
                    await Task.Delay(5000);
                    return;
                }

                ClientInformation clientInformation = new ClientInformation(arguments.Name, arguments.ApiKey);
                StorageClient client = new StorageClient(clientInformation, arguments.StoragePath, arguments.Host, arguments.Port, logger: logger);

                bool connected = false;

                while (!connected)
                {
                    logger.Log("Connecting to server...");
                    connected = await client.StartAsync();

                    Console.WriteLine("Connected to server.\n");

                    if (!connected)
                    {
                        logger.Log("Failed to connect to server.");
                        await Task.Delay(5000);
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
