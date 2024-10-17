using StorageShared.Models;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace StorageShared.Helpers
{
    public static class ExtensionMethods
    {
        public static async Task<ReadMessageTypeResult> ReadMessageTypeAsync(this NetworkStream stream, CancellationToken? cancellationToken)
        {
            byte[] messageTypeBuffer = new byte[1];

            if (cancellationToken == null)
                await stream.ReadAsync(messageTypeBuffer, 0, messageTypeBuffer.Length);
            else
                await stream.ReadAsync(messageTypeBuffer, 0, messageTypeBuffer.Length, cancellationToken.Value);

            return new ReadMessageTypeResult(messageTypeBuffer[0]);
        }

        public static byte[] GetHash(this MD5 md5, byte[] bytes)
        {
            return md5.ComputeHash(bytes);
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

        public static string GetAsString(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
