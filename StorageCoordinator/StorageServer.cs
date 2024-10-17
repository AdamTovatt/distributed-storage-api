using StorageShared.Helpers;
using StorageShared.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StorageCoordinator
{
    public class StorageServer
    {
        public delegate void MessageEventHandler(Message message);
        public event MessageEventHandler? MessageReceived;

        public int Port { get; private set; }
        private TcpListener listener;
        private List<ConnectedClient> clients;

        public ReadOnlyCollection<ConnectedClient> Clients => clients.AsReadOnly();
        public ConnectedClient? PrefferdClient { get { if (clients.Count == 0) return null; return clients[0]; } }

        private Logger logger;
        private CancellationToken? cancellationToken;

        public StorageServer(int port, Logger? logger = null)
        {
            Port = port;
            clients = new List<ConnectedClient>();
            listener = new TcpListener(IPAddress.Any, Port);

            if (logger != null)
                this.logger = logger;
            else
                this.logger = new Logger(false);
        }

        public void StartAcceptingConnections()
        {
            listener.Start();
        }

        public void StopAcceptingConnections()
        {
            listener.Stop();
        }

        public async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
                    Thread handleClientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    handleClientThread.Start(client);
                }
                catch (Exception exception)
                {
                    logger.Log($"Error when accepting client connection: {exception.Message}");
                }
            }

            foreach (ConnectedClient client in clients)
            {
                client.Dispose();
            }
        }

        private void ClientAcceptingThreadstart(object? cancellationToken)
        {
            if (cancellationToken == null)
                throw new Exception("Missing cancellation token when starting client accepting thread");

            Task.Run(async () =>
            {
                await AcceptClientsAsync((CancellationToken)cancellationToken);
            }).Wait();
        }

        public Thread StartClientAcceptingThread(CancellationToken cancellationToken)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(ClientAcceptingThreadstart));
            thread.Start(cancellationToken);
            return thread;
        }

        public async Task Broadcast(Message message)
        {
            List<Task> tasks = new List<Task>();

            foreach (ConnectedClient client in clients)
            {
                tasks.Add(message.WriteMessageAsync(client.Stream));
            }

            await Task.WhenAll(tasks);
        }

        public async void HandleClient(object? obj)
        {
            if (obj == null) throw new Exception("Missing client object when handling client");

            TcpClient tcpClient = (TcpClient)obj;

            using (NetworkStream clientNetworkStream = tcpClient.GetStream())
            {
                ClientInformation? clientInformation = await ReadClientInformationAsync(clientNetworkStream);

                if (clientInformation == null)
                {
                    tcpClient.Close();
                }
                else
                {
                    ConnectedClient client = new ConnectedClient(tcpClient, clientNetworkStream, clientInformation);
                    clients.Add(client);

                    Task.Run(async () =>
                    {
                        while (this.cancellationToken == null || !this.cancellationToken.Value.IsCancellationRequested)
                        {
                            try
                            {
                                ReadMessageTypeResult readMessageTypeResult = await client.Stream.ReadMessageTypeAsync(cancellationToken);

                                if (!readMessageTypeResult.Valid)
                                    continue;

                                MessageType messageType = readMessageTypeResult.MessageType!.Value;

                                if (messageType != MessageType.NoMessage)
                                {
                                    try
                                    {
                                        Message message = await Message.ReadMessageAsync(messageType, client.Stream);

                                        if (message.Type == MessageType.Utf8Encoded)
                                        {
                                            Console.WriteLine(message.GetContentAsString());

                                            await Broadcast(message);
                                        }

                                        MessageReceived?.Invoke(message);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.Message);
                                    }
                                }
                            }
                            catch (IOException ioException)
                            {
                                if (ioException.InnerException is SocketException socketException) // socket exceptions happen for example when the client disconnects
                                {
                                    bool wasClientDisconnect =
                                        socketException.SocketErrorCode == SocketError.ConnectionReset ||
                                        socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                                        socketException.SocketErrorCode == SocketError.Disconnecting;


                                    if (wasClientDisconnect) // client disconnected
                                    {
                                        logger.Log("A client disconnected");
                                        break;
                                    }
                                    else
                                    {
                                        logger.Log($"Socket error code: {socketException.SocketErrorCode}");
                                    }
                                }
                                else
                                {
                                    logger.Log($"IOException: {ioException.Message}");
                                }
                            }
                            catch (Exception exception)
                            {
                                logger.Log($"Error in receiving: {exception.Message}");
                            }
                        }
                    }).Wait();

                    client.Dispose();
                    clients.Remove(client);
                }
            }
        }

        public async Task<ClientInformation?> ReadClientInformationAsync(NetworkStream stream)
        {
            try
            {
                ReadMessageTypeResult readMessageTypeResult = await stream.ReadMessageTypeAsync(cancellationToken);

                MessageType? messageType = readMessageTypeResult.MessageType;
                if (messageType == null || readMessageTypeResult.MessageType != MessageType.Authorization)
                    return null;

                Message message = await Message.ReadMessageAsync(messageType!.Value, stream);
                ClientInformation clientInformation = (ClientInformation)ClientInformation.FromJson(message.Metadata!);

                return clientInformation;
            }
            catch (Exception exception)
            {
                logger.Log($"Error when reading client information: {exception.Message}");
                return null;
            }
        }
    }
}
