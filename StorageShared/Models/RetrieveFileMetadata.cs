using System.Text.Json;

namespace StorageShared.Models
{
    public class RetrieveFileMetadata : IMessageMetadata
    {
        public string OperationId { get; set; }
        public string FileName { get; set; }
        public int ChunkSize { get; set; }

        public RetrieveFileMetadata(string operationId, string fileName, int chunkSize)
        {
            OperationId = operationId;
            FileName = fileName;
            ChunkSize = chunkSize;
        }

        public static IMessageMetadata FromJson(string json)
        {
            RetrieveFileMetadata? metadata = JsonSerializer.Deserialize<RetrieveFileMetadata>(json);

            if (metadata == null)
                throw new JsonException($"Failed to deserialize RetrieveFileMetadata from json: {json}");

            return metadata;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
