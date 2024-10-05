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
        private StorageServer storageServer;
        private int chunkSize;

        public DistributedStorage(StorageServer storageServer, int chunkSize = 1024)
        {
            this.storageServer = storageServer;
        }

        public async Task<bool> StoreDataAsync(string fileName, Stream dataStream)
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

                StoreFileMetadata storeFileMetadata = new StoreFileMetadata(fileName, i, chunks);

                await storageServer.Broadcast(new Message(MessageType.StoreData, data, storeFileMetadata.ToJson()));
            }

            return true;
        }
    }
}
