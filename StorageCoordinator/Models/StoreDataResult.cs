using StorageShared.Models;

namespace StorageCoordinator.Models
{
    public class StoreDataResult
    {
        public List<FileTransferResultMetadata> FileTransferResults { get; set; }

        public StoreDataResult(List<FileTransferResultMetadata> fileTransferResults)
        {
            FileTransferResults = fileTransferResults;
        }
    }
}
