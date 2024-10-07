using System.Text.Json;

namespace StorageShared.Models
{
    public class ClientInformation : IMessageMetadata
    {
        public string Name { get; set; }
        public string ApiKey { get; set; }

        public ClientInformation(string name, string apiKey)
        {
            Name = name;
            ApiKey = apiKey;
        }

        public static IMessageMetadata FromJson(string json)
        {
            ClientInformation? clientInformation = JsonSerializer.Deserialize<ClientInformation>(json);

            if (clientInformation == null)
                throw new JsonException("Failed to deserialize client information");

            return clientInformation;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
