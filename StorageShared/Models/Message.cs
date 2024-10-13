using StorageShared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;

namespace StorageShared.Models
{
    public class Message
    {
        public MessageType Type { get; private set; }
        public int ContentLength { get; private set; }
        public byte[] Content { get; private set; }
        public string? Metadata { get; set; }

        public Message(MessageType type, string metadata)
        {
            Type = type;
            Metadata = metadata;
            Content = new byte[0];
            ContentLength = 0;
        }

        public Message(MessageType type, int contentLength, byte[] content, string? metadata)
        {
            Type = type;
            ContentLength = contentLength;
            Content = content;
            Metadata = metadata;
        }

        public Message(MessageType type, byte[] content, string? metadata)
        {
            Type = type;
            Content = content;
            ContentLength = content.Length;
            Metadata = metadata;
        }

        public static async Task<Message> ReadMessageAsync(MessageType messageType, NetworkStream stream)
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                // first read the metadata length
                byte[] metadataLengthBuffer = new byte[4];
                await stream.ReadExactlyAsync(metadataLengthBuffer, 0, metadataLengthBuffer.Length, cancellationTokenSource.Token);
                int metadataLength = BitConverter.ToInt32(metadataLengthBuffer, 0);

                // then read the content the length
                byte[] contentLengthBuffer = new byte[4];
                await stream.ReadExactlyAsync(contentLengthBuffer, 0, contentLengthBuffer.Length, cancellationTokenSource.Token);
                int contentLength = BitConverter.ToInt32(contentLengthBuffer, 0);

                // then read the amount of bytes that is the metadata
                byte[] metadata = new byte[metadataLength];
                if (metadataLength > 0)
                    await stream.ReadExactlyAsync(metadata, 0, metadata.Length, cancellationTokenSource.Token);

                // then read that amount of bytes to the content
                byte[] content = new byte[contentLength];
                if (contentLength > 0)
                    await stream.ReadExactlyAsync(content, 0, content.Length, cancellationTokenSource.Token);

                using (MD5 md5 = MD5.Create())
                {
                    md5.TransformBlock(new byte[] { (byte)messageType }, 0, 1, null, 0);
                    md5.TransformBlock(metadataLengthBuffer, 0, 4, null, 0);
                    md5.TransformBlock(contentLengthBuffer, 0, 4, null, 0);
                    md5.TransformBlock(metadata, 0, metadata.Length, null, 0);
                    md5.TransformBlock(content, 0, content.Length, null, 0);
                    md5.TransformFinalBlock(new byte[16], 0, 16);

                    byte[] computedMessageHash = md5.Hash!;
                    byte[] readMessageHash = new byte[16];

                    await stream.ReadExactlyAsync(readMessageHash, 0, 16, cancellationTokenSource.Token);

                    if (!computedMessageHash.SequenceEqual(readMessageHash))
                        throw new Exception("Message hash does not match");
                }

                return new Message(messageType, content, Encoding.UTF8.GetString(metadata));
            }
        }

        public async Task WriteMessageAsync(NetworkStream stream)
        {
            byte[] contentLengthBuffer = BitConverter.GetBytes(Content.Length);

            byte[] metadataBytes = Metadata == null ? new byte[0] : Encoding.UTF8.GetBytes(Metadata);
            byte[] metadataLengthBuffer = BitConverter.GetBytes(metadataBytes.Length);

            // 1 byte for message type + 4 bytes for content length + 4 metadatabytes length + the lenght of the metadata + the lenght of the content + 16 bytes for the hash
            byte[] totalMessage = new byte[1 + 4 + 4 + metadataBytes.Length + ContentLength + 16];

            totalMessage[0] = (byte)Type; // set the first byte to the message type
            metadataLengthBuffer.CopyTo(totalMessage, 1); // copy in the metadata length bytes
            contentLengthBuffer.CopyTo(totalMessage, 1 + 4); // copy in the content length bytes
            metadataBytes.CopyTo(totalMessage, 1 + 4 + 4); // copy in the metadata
            Content.CopyTo(totalMessage, 1 + 4 + 4 + metadataBytes.Length); // copy in the content

            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(totalMessage);

                hashBytes.CopyTo(totalMessage, 1 + 4 + 4 + metadataBytes.Length + ContentLength);

                Console.WriteLine("Writing hash: " + hashBytes.GetAsString());

                await stream.WriteAsync(totalMessage, 0, totalMessage.Length);
            }
        }

        public string GetContentAsString()
        {
            return Encoding.UTF8.GetString(Content);
        }
    }
}
