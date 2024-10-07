namespace StorageShared.Models
{
    public enum MessageType
    {
        NoMessage = 0,
        Utf8Encoded = 1,
        StoreData = 2,
        TransferDataResult = 3,
        RetrieveData = 4,
        RetrieveFileInfoRequest = 5,
        FileInfo = 6,
    }
}
