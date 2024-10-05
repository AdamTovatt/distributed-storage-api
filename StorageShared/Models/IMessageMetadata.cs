namespace StorageShared.Models
{
    public interface IMessageMetadata
    {
        public static abstract IMessageMetadata FromJson(string json);
        public string ToJson();
    }
}
