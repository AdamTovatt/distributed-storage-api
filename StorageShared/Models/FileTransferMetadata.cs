using System.Text.Json;

namespace StorageShared.Models
{
    public class FileTransferMetadata : IMessageMetadata
    {
        public string FileName { get; set; }
        public int PartIndex { get; set; }
        public int TotalParts { get; set; }
        public int PartSize { get; set; }
        public string OperationId { get; set; }

        public FileTransferMetadata(string fileName, int partIndex, int totalParts, int partSize, string operationId)
        {
            FileName = fileName;
            PartIndex = partIndex;
            TotalParts = totalParts;
            PartSize = partSize;
            OperationId = operationId;
        }

        public static IMessageMetadata FromJson(string json)
        {
            FileTransferMetadata? metadata = JsonSerializer.Deserialize<FileTransferMetadata>(json);

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
