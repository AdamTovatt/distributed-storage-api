using StorageShared.Helpers;
using StorageShared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Security.Cryptography;
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

        public ClientInformation ClientInformation { get; private set; }

        public int Port { get; set; }
        public string HostName { get; set; }
        public string StoragePath { get; set; }

        private TcpClient? client;
        private NetworkStream? stream;
        public bool Running { get; private set; }

        private CancellationToken? cancellationToken;

        public StorageClient(ClientInformation clientInformation, string storagePath, string hostName, int port, CancellationToken? cancellationToken = null, Logger? logger = null)
        {
            ClientInformation = clientInformation;

            StoragePath = storagePath;
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

                Message authorizationMessage = new Message(MessageType.Authorization, ClientInformation.ToJson());
                await authorizationMessage.WriteMessageAsync(stream!);
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
                            else if (message.Type == MessageType.StoreData) // store data
                            {
                                FileTransferMetadata storeFileMetadata = (FileTransferMetadata)FileTransferMetadata.FromJson(message.Metadata!);

                                if (storeFileMetadata.PartIndex == 0)
                                {
                                    if (File.Exists(LocalizeFileName(storeFileMetadata.FileName)))
                                        File.Delete(LocalizeFileName(storeFileMetadata.FileName));
                                }

                                FileMode fileMode = File.Exists(LocalizeFileName(storeFileMetadata.FileName)) ? FileMode.Append : FileMode.Create;

                                using (FileStream fileStream = new FileStream(LocalizeFileName(storeFileMetadata.FileName), FileMode.Append))
                                    fileStream.Write(message.Content, 0, message.Content.Length);

                                if (storeFileMetadata.PartIndex == storeFileMetadata.TotalParts - 1)
                                {
                                    logger.Log($"File {storeFileMetadata.FileName} stored");

                                    string fileHash = await FileHash.GetAsStringAsync(LocalizeFileName(storeFileMetadata.FileName));
                                    FileTransferResultMetadata resultMetadata = new FileTransferResultMetadata(storeFileMetadata.OperationId, true, "File stored", fileHash);
                                    await SendMessageAsync(new Message(MessageType.TransferDataResult, resultMetadata.ToJson()));
                                }
                            }
                            else if (message.Type == MessageType.RetrieveData) // retrieve data
                            {
                                RetrieveFileMetadata retrieveFileMetadata = (RetrieveFileMetadata)RetrieveFileMetadata.FromJson(message.Metadata!);

                                if (!File.Exists(LocalizeFileName(retrieveFileMetadata.FileName)))
                                {
                                    FileTransferMetadata fileTransferMetadata = new FileTransferMetadata(retrieveFileMetadata.FileName, -1, -1, -1, retrieveFileMetadata.OperationId);
                                    await SendMessageAsync(new Message(MessageType.TransferDataResult, fileTransferMetadata.ToJson()));
                                }
                                else
                                {
                                    using (FileStream fileStream = new FileStream(LocalizeFileName(retrieveFileMetadata.FileName), FileMode.Open))
                                    {
                                        int chunks = (int)Math.Ceiling((double)new FileInfo(LocalizeFileName(retrieveFileMetadata.FileName)).Length / retrieveFileMetadata.ChunkSize);

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
                            else if (message.Type == MessageType.RetrieveFileInfoRequest)
                            {
                                GetStoredFileInfoMetadata requestMetadata = (GetStoredFileInfoMetadata)GetStoredFileInfoMetadata.FromJson(message.Metadata!);

                                StoredFileInfo? storedFileInfo = null;

                                if (File.Exists(requestMetadata.FileName))
                                {
                                    long fileLength = -1;
                                    string? hash = null;

                                    using (MD5 md5 = MD5.Create())
                                    using (FileStream fileStream = File.OpenRead(requestMetadata.FileName))
                                    {
                                        fileLength = fileStream.Length;

                                        await Task.Run(() =>
                                        {
                                            hash = md5.GetHashAsString(fileStream);
                                        });
                                    }

                                    storedFileInfo = new StoredFileInfo(
                                        requestMetadata.FileName,
                                        File.GetCreationTimeUtc(requestMetadata.FileName),
                                        fileLength,
                                        hash ?? "missing file hash"
                                        );
                                }

                                ReturnFileInfoMetadata returnMetadata = new ReturnFileInfoMetadata(requestMetadata.OperationId, storedFileInfo);
                                await SendMessageAsync(new Message(MessageType.FileInfo, returnMetadata.ToJson()));
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

        private string LocalizeFileName(string fileName)
        {
            return Path.Combine(StoragePath, fileName);
        }
    }
}
