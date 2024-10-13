using StorageShared.Helpers;
using StorageShared.Models;

namespace StorageClient
{
    internal class ClientStart
    {
        static async Task Main(string[] args)
        {
            Logger logger = new Logger(true);

            ClientArguments? arguments = ClientArguments.ReadFromRuntimeArguments(args, logger);
            if (arguments == null)
            {
                await Task.Delay(5000);
                return;
            }

            ClientInformation clientInformation = new ClientInformation(arguments.Name, arguments.ApiKey);

            while (true)
            {
                StorageClient client = new StorageClient(clientInformation, arguments.StoragePath, arguments.Host, arguments.Port, logger: logger);

                while (true)
                {
                    logger.Log($"Connecting to server at {client.HostName}:{client.Port}");

                    if (await client.StartAsync())
                    {
                        logger.Log("Connected successfully.");
                        break;
                    }
                    else
                    {
                        logger.Log("Failed to connect to server.");
                        await Task.Delay(5000);
                    }
                }

                while (true)
                {
                    await Task.Delay(500);

                    if (!client.Running)
                    {
                        logger.Log("Connection to server has been lost, restarting");
                        client.Dispose();
                        break;
                    }
                }
            }
        }
    }
}
