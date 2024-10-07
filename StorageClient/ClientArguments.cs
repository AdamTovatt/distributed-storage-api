using StorageShared.Helpers;

namespace StorageClient
{
    internal class ClientArguments
    {
        internal string Name { get; set; }
        internal string StoragePath { get; set; }
        internal string ApiKey { get; set; }
        internal string Host { get; set; }
        internal int Port { get; set; }

        internal ClientArguments(string name, string storagePath, string apiKey, string serverAddress, int serverPort)
        {
            Name = name;
            StoragePath = storagePath;
            ApiKey = apiKey;
            Host = serverAddress;
            Port = serverPort;
        }

        internal static ClientArguments? ReadFromRuntTimeArguments(string[] args, Logger logger)
        {
            Dictionary<string, string> parsedArguments = RuntimeArgumentsReader.CreateDictionary(args);

            bool missingKey = false;

            if (!parsedArguments.ContainsKey("name"))
            {
                logger.Log("Missing name parameter, should be specified as run time argument like this: --name the-name");
                missingKey = true;
            }

            if (!parsedArguments.ContainsKey("storage-path"))
            {
                logger.Log("Missing storage-path parameter, should be specified as run time argument like this: --storage-path the-path");
                missingKey = true;
            }

            if (!parsedArguments.ContainsKey("api-key"))
            {
                logger.Log("Missing api-key parameter, should be specified as run time argument like this: api-key");
                missingKey = true;
            }

            if (!parsedArguments.ContainsKey("host"))
            {
                logger.Log("Missing host parameter, should be specified as run time argument like this: --host the-hosting-server-address");
                missingKey = true;
            }

            if (!parsedArguments.ContainsKey("port"))
            {
                logger.Log("Missing port parameter, should be specified as run time argument like this: --server-port the-hosting-server-port");
                missingKey = true;
            }

            if (missingKey)
                return null;

            try
            {
                string name = parsedArguments["name"];
                string storagePath = parsedArguments["storage-path"];
                string apiKey = parsedArguments["api-key"];
                string host = parsedArguments["host"];
                int port = int.Parse(parsedArguments["port"]);

                return new ClientArguments(name, storagePath, apiKey, host, port);
            }
            catch (Exception exception)
            {
                logger.Log($"Error when reading run time arguments: {exception.Message}");
                return null;
            }
        }
    }
}
