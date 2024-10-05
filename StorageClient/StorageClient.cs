using StorageShared.Helpers;
using StorageShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace StorageClient
{
    public class StorageClient
    {
        public delegate void MessageEventHandler(Message message);
        public event MessageEventHandler? MessageReceived;

        public int Port { get; set; }
        public string HostName { get; set; }

        private TcpClient? client;
        private NetworkStream? stream;
        private bool running;

        private CancellationToken? cancellationToken;

        public StorageClient(string hostName, int port, CancellationToken? cancellationToken = null)
        {
            Port = port;
            HostName = hostName;
            this.cancellationToken = cancellationToken;
        }

        public async Task StartAsync()
        {
            await Task.Run(() =>
            {
                client = new TcpClient(HostName, Port);
            });

            stream = client?.GetStream();

            running = true;

            Thread handleIncommingMessagesThread = new Thread(HandleIncommingMessages);
            handleIncommingMessagesThread.Start();
        }

        private void HandleIncommingMessages()
        {
            Task.Run(async () =>
            {
                while (running && !(cancellationToken != null && !cancellationToken.Value.IsCancellationRequested))
                {
                    MessageType messageType = await stream!.ReadMessageTypeAsync();

                    if (messageType != MessageType.NoMessage)
                    {
                        Message message = await Message.ReadMessageAsync(messageType, stream!);

                        if (message.Type == MessageType.Utf8Encoded)
                        {
                            Console.WriteLine(message.GetContentAsString());
                        }
                    }
                }
            }).Wait();
        }

        public void Stop()
        {
            running = false;
        }

        public async Task SendMessageAsync(Message message)
        {
            if (!running)
                throw new Exception("Can not send message without starting client first");

            await message.WriteMessageAsync(stream!);
        }
    }
}
