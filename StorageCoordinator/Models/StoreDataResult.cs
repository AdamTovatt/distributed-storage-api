using StorageShared.Models;

namespace StorageCoordinator.Models
{
    public class StoreDataResult
    {
        public List<FileTransferResultMetadata> FileTransferResults { get; private set; }
        public int ClientsOnline { get; private set; }
        public string TransferredDataHash { get; private set; }
        public bool AllHashesMatch { get; private set; }
        public long FileLength { get; private set; }
        public long BytesSent { get; private set; }

        public StoreDataResult(List<FileTransferResultMetadata> fileTransferResults, int clientsOnline, string transferredDataHash, long fileLength, long bytesSent)
        {
            FileTransferResults = fileTransferResults;
            ClientsOnline = clientsOnline;
            TransferredDataHash = transferredDataHash;

            FileLength = fileLength;
            BytesSent = bytesSent;

            AllHashesMatch = true;
            foreach (FileTransferResultMetadata result in fileTransferResults)
            {
                if (result.Hash != transferredDataHash)
                {
                    AllHashesMatch = false;
                    break;
                }
            }
        }
    }
}
