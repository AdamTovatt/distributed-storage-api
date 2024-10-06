using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
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
            // first read the metadata length
            byte[] metadataLengthBuffer = new byte[4];
            await stream.ReadAsync(metadataLengthBuffer, 0, metadataLengthBuffer.Length);
            int metadataLength = BitConverter.ToInt32(metadataLengthBuffer, 0);

            // then read the content the length
            byte[] contentLengthBuffer = new byte[4];
            await stream.ReadAsync(contentLengthBuffer, 0, contentLengthBuffer.Length);
            int contentLength = BitConverter.ToInt32(contentLengthBuffer, 0);

            // then read the amount of bytes that is the metadata
            byte[] metadata = new byte[metadataLength];
            if (metadataLength > 0)
                await stream.ReadAsync(metadata, 0, metadata.Length);

            // then read that amount of bytes to the content
            byte[] content = new byte[contentLength];
            if (contentLength > 0)
                await stream.ReadAsync(content, 0, content.Length);

            return new Message(messageType, content, Encoding.UTF8.GetString(metadata));
        }

        public async Task WriteMessageAsync(NetworkStream stream)
        {
            byte[] contentLengthBuffer = BitConverter.GetBytes(Content.Length);

            byte[] metadataBytes = Metadata == null ? new byte[0] : Encoding.UTF8.GetBytes(Metadata);
            byte[] metadataLengthBuffer = BitConverter.GetBytes(metadataBytes.Length);

            byte[] totalMessage = new byte[1 + 4 + 4 + metadataBytes.Length + ContentLength]; // 1 byte for message type + 4 bytes for content length + 4 metadatabytes length

            totalMessage[0] = (byte)Type; // set the first byte to the message type
            metadataLengthBuffer.CopyTo(totalMessage, 1); // copy in the metadata length bytes
            contentLengthBuffer.CopyTo(totalMessage, 1 + 4); // copy in the content length bytes
            metadataBytes.CopyTo(totalMessage, 1 + 4 + 4); // copy in the metadata
            Content.CopyTo(totalMessage, 1 + 4 + 4 + metadataBytes.Length); // copy in the content

            await stream.WriteAsync(totalMessage, 0, totalMessage.Length);
        }

        public string GetContentAsString()
        {
            return Encoding.UTF8.GetString(Content);
        }
    }
}
