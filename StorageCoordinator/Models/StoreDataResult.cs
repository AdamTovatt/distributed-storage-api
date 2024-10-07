using StorageShared.Models;

namespace StorageCoordinator.Models
{
    public class StoreDataResult
    {
        public List<FileTransferResultMetadata> FileTransferResults { get; set; }
        public int ClientsOnline { get; set; }

        public StoreDataResult(List<FileTransferResultMetadata> fileTransferResults, int clientsOnline)
        {
            FileTransferResults = fileTransferResults;
            ClientsOnline = clientsOnline;
        }
    }
}
