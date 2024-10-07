using StorageShared.Models;

namespace StorageCoordinator.Models
{
    public class GetStoredFileInfoResult
    {
        public int ClientsOnline { get; set; }
        public List<StoredFileInfo> StoredFiles { get; set; }

        public GetStoredFileInfoResult(int clientsOnline, List<StoredFileInfo> storedFiles)
        {
            ClientsOnline = clientsOnline;
            StoredFiles = storedFiles;
        }
    }
}
