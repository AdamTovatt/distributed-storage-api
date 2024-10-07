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
        public delegate void DisconnectedEventHandler();
        public event DisconnectedEventHandler? Disconnected;

        public delegate void MessageEventHandler(Message message);
        public event MessageEventHandler? MessageReceived;

        private Logger logger;

        public int Port { get; set; }
        public string HostName { get; set; }

        private TcpClient? client;
        private NetworkStream? stream;
        public bool Running { get; private set; }

        private CancellationToken? cancellationToken;

        public StorageClient(string hostName, int port, CancellationToken? cancellationToken = null, Logger? logger = null)
        {
            Port = port;
            HostName = hostName;
            this.cancellationToken = cancellationToken;

            if (logger != null)
                this.logger = logger;
            else
                this.logger = new Logger(false);
        }

        public async Task<bool> StartAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    client = new TcpClient(HostName, Port);
                });

                stream = client?.GetStream();

                Running = true;

                Thread handleIncommingMessagesThread = new Thread(HandleIncommingMessages);
                handleIncommingMessagesThread.Start();
            }
            catch (SocketException exception)
            {
                logger.Log($"Error when connecting to server: {exception.Message}");

                return false;
            }

            return true;
        }

        private void HandleIncommingMessages()
        {
            Task.Run(async () =>
            {
                while (Running && !(cancellationToken != null && !cancellationToken.Value.IsCancellationRequested))
                {
                    try
                    {
                        MessageType messageType = await stream!.ReadMessageTypeAsync();

                        if (messageType != MessageType.NoMessage)
                        {
                            Message message = await Message.ReadMessageAsync(messageType, stream!);

                            if (message.Type == MessageType.Utf8Encoded)
                            {
                                logger.Log($"Got message: {message.GetContentAsString()}");
                            }
                            else if (message.Type == MessageType.StoreData)
                            {
                                FileTransferMetadata storeFileMetadata = (FileTransferMetadata)FileTransferMetadata.FromJson(message.Metadata!);

                                if (storeFileMetadata.PartIndex == 0)
                                {
                                    if (File.Exists(storeFileMetadata.FileName))
                                        File.Delete(storeFileMetadata.FileName);
                                }

                                FileMode fileMode = File.Exists(storeFileMetadata.FileName) ? FileMode.Append : FileMode.Create;

                                using (FileStream fileStream = new FileStream(storeFileMetadata.FileName, FileMode.Append))
                                    fileStream.Write(message.Content, 0, message.Content.Length);

                                if (storeFileMetadata.PartIndex == storeFileMetadata.TotalParts - 1)
                                {
                                    logger.Log($"File {storeFileMetadata.FileName} stored");

                                    FileTransferResultMetadata resultMetadata = new FileTransferResultMetadata(storeFileMetadata.OperationId, true, "File stored");
                                    await SendMessageAsync(new Message(MessageType.TransferDataResult, resultMetadata.ToJson()));
                                }
                            }
                            else if (message.Type == MessageType.RetrieveData)
                            {
                                RetrieveFileMetadata retrieveFileMetadata = (RetrieveFileMetadata)RetrieveFileMetadata.FromJson(message.Metadata!);

                                if (!File.Exists(retrieveFileMetadata.FileName))
                                {
                                    FileTransferMetadata fileTransferMetadata = new FileTransferMetadata(retrieveFileMetadata.FileName, -1, -1, -1, retrieveFileMetadata.OperationId);
                                    await SendMessageAsync(new Message(MessageType.TransferDataResult, fileTransferMetadata.ToJson()));
                                }
                                else
                                {
                                    using (FileStream fileStream = new FileStream(retrieveFileMetadata.FileName, FileMode.Open))
                                    {
                                        int chunks = (int)Math.Ceiling((double)new FileInfo(retrieveFileMetadata.FileName).Length / retrieveFileMetadata.ChunkSize);

                                        for (int i = 0; i < chunks; i++)
                                        {
                                            byte[] data = new byte[retrieveFileMetadata.ChunkSize];
                                            int bytesRead = fileStream.Read(data, 0, retrieveFileMetadata.ChunkSize);

                                            if (bytesRead == 0)
                                            {
                                                break;
                                            }

                                            FileTransferMetadata fileTransferMetadata = new FileTransferMetadata(retrieveFileMetadata.FileName, i, chunks, retrieveFileMetadata.ChunkSize, retrieveFileMetadata.OperationId);

                                            await SendMessageAsync(new Message(MessageType.TransferDataResult, data, fileTransferMetadata.ToJson()));
                                        }
                                    }
                                }
                            }

                            MessageReceived?.Invoke(message);
                        }
                    }
                    catch (IOException ioException)
                    {
                        if (ioException.InnerException is SocketException socketException) // socket exceptions happen for example when the server disconnects
                        {
                            bool wasServerDisconnect =
                                socketException.SocketErrorCode == SocketError.ConnectionReset ||
                                socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                                socketException.SocketErrorCode == SocketError.Disconnecting;


                            if (wasServerDisconnect) // server disconnected
                            {
                                logger.Log($"Server disconnected: {socketException.Message}");
                                break;
                            }
                            else
                            {
                                logger.Log($"Socket error code: {socketException.SocketErrorCode}");
                                break;
                            }
                        }
                        else
                        {
                            logger.Log($"IOException: {ioException.Message}");
                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.Log($"Error in receiving: {exception.Message}");
                        logger.Log(exception.StackTrace ?? "missing stack trace");
                    }
                }
            }).Wait();

            Disconnected?.Invoke();
            Stop();
        }

        public void Stop()
        {
            Running = false;
        }

        public async Task SendMessageAsync(Message message)
        {
            if (!Running)
                throw new Exception("Can not send message without starting client first");

            await message.WriteMessageAsync(stream!);
        }
    }
}
