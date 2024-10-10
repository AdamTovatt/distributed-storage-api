using StorageShared.Models;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace StorageShared.Helpers
{
    public static class ExtensionMethods
    {
        public static async Task<MessageType> ReadMessageTypeAsync(this NetworkStream stream, bool throwOnInvalidType = true)
        {
            byte[] messageTypeBuffer = new byte[1];
            await stream.ReadAsync(messageTypeBuffer, 0, messageTypeBuffer.Length);

            MessageType type = (MessageType)messageTypeBuffer[0];

            if (!Enum.IsDefined(typeof(MessageType), type) && throwOnInvalidType)
            {
                throw new Exception($"Invalid message type: {type}");
            }

            return type;
        }

        public static string GetHashAsString(this MD5 md5, Stream stream)
        {
            byte[] hashBytes = md5.ComputeHash(stream);
            return hashBytes.GetAsString();
        }

        public static string GetHashAsString(this MD5 md5)
        {
            byte[] hashBytes = md5.Hash!;
            return hashBytes.GetAsString();
        }

        private static string GetAsString(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
