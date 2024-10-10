using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StorageShared.Models
{
    public class FileTransferResultMetadata : IMessageMetadata
    {
        public string OperationId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Hash { get; set; }

        public FileTransferResultMetadata(string operationId, bool success, string message, string hash)
        {
            OperationId = operationId;
            Success = success;
            Message = message;
            Hash = hash;
        }

        public static IMessageMetadata FromJson(string json)
        {
            FileTransferResultMetadata? metadata = JsonSerializer.Deserialize<FileTransferResultMetadata>(json);

            if (metadata == null)
                throw new JsonException($"Failed to deserialize FileTransferResultMetadata from json: {json}");

            return metadata;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
