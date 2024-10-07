using StorageCoordinator.Models;
using StorageShared.Models;
using System.Net;

namespace StorageCoordinator
{
    public class DistributedStorage
    {
        public static DistributedStorage Instance { get { if (instance == null) throw new Exception("Distributed storage must be initialized before it is used!"); return instance; } }
        private static DistributedStorage? instance;

        private StorageServer storageServer;
        private int chunkSize;

        public static DistributedStorage Initialize(StorageServer storageServer, int chunkSize = 1024)
        {
            instance = new DistributedStorage(storageServer, chunkSize);

            return instance;
        }

        private DistributedStorage(StorageServer storageServer, int chunkSize = 1024)
        {
            this.storageServer = storageServer;
            this.chunkSize = chunkSize;
        }

        public List<ClientInformation> GetOnlineClients()
        {
            List<ClientInformation> clients = new List<ClientInformation>();

            foreach (ConnectedClient client in storageServer.Clients)
            {
                clients.Add(client.ClientInformation);
            }

            return clients;
        }

        public async Task<RetrieveDataResult> RetrieveDataAsync(string fileName, Stream dataStream, CancellationToken cancellationToken)
        {
            string operationId = Guid.NewGuid().ToString();

            RetrieveFileMetadata retrieveFileMetadata = new RetrieveFileMetadata(operationId, fileName, chunkSize); // create message to start trasnfer from client
            Message retrieveMessage = new Message(MessageType.RetrieveData, retrieveFileMetadata.ToJson());

            ConnectedClient? connectedClient = storageServer.PrefferdClient; // get a client

            if (connectedClient == null)
                throw new Exception("No connected clients");

            RetrieveDataResult? result = null;
            int totalParts = -1;
            int partsTransferred = 0;

            StorageServer.MessageEventHandler messageHandler = (Message message) => // set up handling of transfer parts
            {
                if (message.Type == MessageType.TransferDataResult)
                {
                    FileTransferMetadata metadata = (FileTransferMetadata)FileTransferMetadata.FromJson(message.Metadata!);

                    if (metadata.TotalParts == -1) // no file found
                        result = new RetrieveDataResult(false, $"No such file: {fileName}", HttpStatusCode.NoContent);

                    if (metadata.OperationId == operationId)
                    {
                        totalParts = metadata.TotalParts;
                        partsTransferred++;
                        dataStream.WriteAsync(message.Content, 0, message.Content.Length);
                    }
                }
            };

            storageServer.MessageReceived += messageHandler;

            try
            {
                await connectedClient.SendMessageAsync(retrieveMessage); // start the transfer from the client

                while (partsTransferred < totalParts || totalParts == -1) // wait for all parts to be transferred
                {
                    await Task.Delay(100);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        storageServer.MessageReceived -= messageHandler;
                        return new RetrieveDataResult(false, "Transfer was cancelled", HttpStatusCode.BadRequest);
                    }

                    if (result != null)
                        return result;
                }

                return new RetrieveDataResult(true, "Transfer completed", HttpStatusCode.OK);
            }
            catch
            {
                throw;
            }
            finally
            {
                storageServer.MessageReceived -= messageHandler;
            }
        }

        public async Task<StoreDataResult> StoreDataAsync(string fileName, Stream dataStream)
        {
            string operationId = Guid.NewGuid().ToString();

            List<FileTransferResultMetadata> results = new List<FileTransferResultMetadata>();

            StorageServer.MessageEventHandler messageHandler = (Message message) =>
            {
                if (message.Type == MessageType.TransferDataResult)
                {
                    FileTransferResultMetadata metadata = (FileTransferResultMetadata)FileTransferResultMetadata.FromJson(message.Metadata!);

                    if (metadata.OperationId == operationId)
                        results.Add(metadata);
                }
            };

            int onlineClients = storageServer.Clients.Count;
            storageServer.MessageReceived += messageHandler;

            try
            {
                int chunks = (int)Math.Ceiling((double)dataStream.Length / chunkSize);

                for (int i = 0; i < chunks; i++)
                {
                    byte[] data = new byte[chunkSize];
                    int bytesRead = await dataStream.ReadAsync(data, 0, chunkSize);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    FileTransferMetadata storeFileMetadata = new FileTransferMetadata(fileName, i, chunks, chunkSize, operationId);

                    await storageServer.Broadcast(new Message(MessageType.StoreData, data, storeFileMetadata.ToJson()));
                }

                DateTime completedTime = DateTime.Now;

                while (results.Count < storageServer.Clients.Count)
                {
                    await Task.Delay(100);

                    if (DateTime.Now - completedTime > TimeSpan.FromSeconds(10))
                    {
                        throw new TimeoutException("Timeout when waiting for all clients to respond");
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                storageServer.MessageReceived -= messageHandler;
            }

            return new StoreDataResult(results, onlineClients);
        }

        public async Task<GetStoredFileInfoResult> GetStoredFileInfoAsync(string fileName)
        {
            string operationId = Guid.NewGuid().ToString();

            List<StoredFileInfo?> filesInfos = new List<StoredFileInfo?>();

            GetStoredFileInfoMetadata messageMetadata = new GetStoredFileInfoMetadata(operationId, fileName);
            Message messageToSend = new Message(MessageType.RetrieveFileInfoRequest, messageMetadata.ToJson());

            StorageServer.MessageEventHandler responseHandler = (Message message) =>
            {
                if (message.Type == MessageType.FileInfo)
                {
                    ReturnFileInfoMetadata metadata = (ReturnFileInfoMetadata)ReturnFileInfoMetadata.FromJson(message.Metadata!);

                    if (metadata.OperationId == operationId)
                    {
                        filesInfos.Add(metadata.FileInfo);
                    }
                }
            };

            storageServer.MessageReceived += responseHandler;

            try
            {
                DateTime requestTime = DateTime.Now;
                int clientCount = storageServer.Clients.Count;
                await storageServer.Broadcast(messageToSend);

                while (filesInfos.Count < clientCount)
                {
                    await Task.Delay(100);

                    if (DateTime.Now - requestTime > TimeSpan.FromSeconds(10))
                    {
                        break;
                    }
                }

                List<StoredFileInfo> resultList = new List<StoredFileInfo>();

                foreach (StoredFileInfo? fileInfo in filesInfos)
                {
                    if (fileInfo != null)
                        resultList.Add(fileInfo);
                }

                GetStoredFileInfoResult result = new GetStoredFileInfoResult(clientCount, resultList);

                return result;
            }
            catch
            {
                throw;
            }
            finally
            {
                storageServer.MessageReceived -= responseHandler;
            }
        }
    }
}
