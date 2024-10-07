using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StorageShared.Models
{
    public class GetStoredFileInfoMetadata : IMessageMetadata
    {
        public string OperationId { get; set; }
        public string FileName { get; set; }

        public GetStoredFileInfoMetadata(string operationId, string fileName)
        {
            OperationId = operationId;
            FileName = fileName;
        }

        public static IMessageMetadata FromJson(string json)
        {
            GetStoredFileInfoMetadata? result = JsonSerializer.Deserialize<GetStoredFileInfoMetadata>(json);

            if (result == null)
                throw new Exception($"Error when parsing json for GetStoredFileInfoMetadata: {json}");

            return result;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
