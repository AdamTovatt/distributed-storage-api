using System.Text.Json;

namespace StorageShared.Models
{
    public class StoreFileMetadata : IMessageMetadata
    {
        public string FileName { get; set; }
        public int PartIndex { get; set; }
        public int TotalParts { get; set; }
        public int PartSize { get; set; }
        public string OperationId { get; set; }

        public StoreFileMetadata(string fileName, int partIndex, int totalParts, int partSize, string operationId)
        {
            FileName = fileName;
            PartIndex = partIndex;
            TotalParts = totalParts;
            PartSize = partSize;
            OperationId = operationId;
        }

        public static IMessageMetadata FromJson(string json)
        {
            StoreFileMetadata? metadata = JsonSerializer.Deserialize<StoreFileMetadata>(json);

            if (metadata == null)
                throw new JsonException($"Failed to deserialize StoreFileMetadata from json: {json}");

            return metadata;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
