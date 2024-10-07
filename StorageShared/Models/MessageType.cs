namespace StorageShared.Models
{
    public enum MessageType
    {
        NoMessage = 0,
        Authorization = 1,
        Utf8Encoded = 2,
        StoreData = 3,
        TransferDataResult = 4,
        RetrieveData = 5,
        RetrieveFileInfoRequest = 6,
        FileInfo = 7,
    }
}
