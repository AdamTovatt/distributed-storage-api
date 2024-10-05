using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace StorageShared.Models
{
    public class Message
    {
        public MessageType Type { get; private set; }
        public int ContentLength { get; private set; }
        public byte[] Content { get; private set; }

        public Message(MessageType type, int contentLength, byte[] content)
        {
            Type = type;
            ContentLength = contentLength;
            Content = content;
        }

        public Message(MessageType type, byte[] content)
        {
            Type = type;
            Content = content;
            ContentLength = content.Length;
        }

        public static async Task<Message> ReadMessageAsync(MessageType messageType, NetworkStream stream)
        {
            // first read the length
            byte[] contentLengthBuffer = new byte[4];
            await stream.ReadAsync(contentLengthBuffer, 0, contentLengthBuffer.Length);
            int contentLength = BitConverter.ToInt32(contentLengthBuffer, 0);

            // then read that amount of bytes to the content
            byte[] content = new byte[contentLength];
            await stream.ReadAsync(content, 0, content.Length);

            return new Message(messageType, content);
        }

        public async Task WriteMessageAsync(NetworkStream stream)
        {
            byte[] contentLengthBuffer = BitConverter.GetBytes(Content.Length);

            byte[] totalMessage = new byte[1 + 4 + ContentLength]; // 1 byte for message type + 4 bytes for content length + content length
            
            totalMessage[0] = (byte)Type; // set the first byte to the message type
            contentLengthBuffer.CopyTo(totalMessage, 1); // copy in the content length bytes
            Content.CopyTo(totalMessage, 1 + contentLengthBuffer.Length); // copy in the content

            await stream.WriteAsync(totalMessage, 0, totalMessage.Length);
        }

        public string GetContentAsString()
        {
            return Encoding.UTF8.GetString(Content);
        }
    }
}
