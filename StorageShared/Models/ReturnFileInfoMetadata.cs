using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StorageShared.Models
{
    public class ReturnFileInfoMetadata : IMessageMetadata
    {
        public string OperationId { get; set; }
        public StoredFileInfo? FileInfo { get; set; }

        public ReturnFileInfoMetadata(string operationId, StoredFileInfo? fileInfo)
        {
            OperationId = operationId;
            FileInfo = fileInfo;
        }

        public static IMessageMetadata FromJson(string json)
        {
            ReturnFileInfoMetadata? result = JsonSerializer.Deserialize<ReturnFileInfoMetadata>(json);

            if (result == null)
                throw new Exception($"Error when deserializing ReturnFileInfoMetadata: {json}");

            return result;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
