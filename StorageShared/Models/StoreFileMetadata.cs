using System.Text.Json;

namespace StorageShared.Models
{
    public class StoreFileMetadata : IMessageMetadata<StoreFileMetadata>
    {
        public string FileName { get; set; }
        public int PartIndex { get; set; }
        public int TotalParts { get; set; }

        public StoreFileMetadata(string fileName, int partIndex, int totalParts)
        {
            FileName = fileName;
            PartIndex = partIndex;
            TotalParts = totalParts;
        }

        public StoreFileMetadata FromJson(string json)
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
