using StorageShared.Models;
using System.Net.Sockets;

namespace StorageShared.Helpers
{
    public static class ExtensionMethods
    {
        public static async Task<MessageType> ReadMessageTypeAsync(this NetworkStream stream)
        {
            byte[] messageTypeBuffer = new byte[1];
            await stream.ReadAsync(messageTypeBuffer, 0, messageTypeBuffer.Length);

            MessageType type = (MessageType)messageTypeBuffer[0];

            if (!Enum.IsDefined(typeof(MessageType), type))
            {
                throw new Exception($"Invalid message type: {type}");
            }

            return type;
        }
    }
}
