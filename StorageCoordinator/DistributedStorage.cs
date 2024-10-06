﻿using StorageCoordinator.Models;
using StorageShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                    StoreFileMetadata storeFileMetadata = new StoreFileMetadata(fileName, i, chunks, chunkSize, operationId);

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

            return new StoreDataResult(results);
        }
    }
}
