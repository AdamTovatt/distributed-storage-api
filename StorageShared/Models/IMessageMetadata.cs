namespace StorageShared.Models
{
    public interface IMessageMetadata<T> where T : class
    {
        public T FromJson(string json);
        public string ToJson();
    }
}
