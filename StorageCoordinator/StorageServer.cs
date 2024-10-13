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

        public StorageServer(int port)
        {
            Port = port;
            clients = new List<ConnectedClient>();
            listener = new TcpListener(IPAddress.Any, Port);
        }

        public void StartAcceptingConnections()
        {
            listener.Start();
        }

        public async Task AcceptClientsAsync(CancellationToken? cancellationToken = null)
        {
            while (cancellationToken == null || !cancellationToken.Value.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Thread handleClientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    handleClientThread.Start(client);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error when accepting client connection: {exception.Message}");
                }
            }

            foreach (ConnectedClient client in clients)
            {
                client.Dispose();
            }
        }

        private void ClientAcceptingThreadstart()
        {
            Task.Run(async () =>
            {
                await AcceptClientsAsync();
            }).Wait();
        }

        public Thread CreateClientAcceptingThread()
        {
            return new Thread(ClientAcceptingThreadstart);
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
                        while (true)
                        {
                            try
                            {
                                ReadMessageTypeResult readMessageTypeResult = await client.Stream.ReadMessageTypeAsync();

                                if (!readMessageTypeResult.Valid)
                                {
                                    Console.WriteLine($"Invalid message type: {readMessageTypeResult.RawValue}");
                                    continue;
                                }
                                else
                                    Console.WriteLine($"Message type: {readMessageTypeResult.RawValue}");

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
                                        Console.WriteLine("A client disconnected");
                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Socket error code: {socketException.SocketErrorCode}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"IOException: {ioException.Message}");
                                }
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine($"Error in receiving: {exception.Message}");
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
                ReadMessageTypeResult readMessageTypeResult = await stream.ReadMessageTypeAsync();

                MessageType? messageType = readMessageTypeResult.MessageType;
                if (messageType == null || readMessageTypeResult.MessageType != MessageType.Authorization)
                    return null;

                Message message = await Message.ReadMessageAsync(messageType!.Value, stream);
                ClientInformation clientInformation = (ClientInformation)ClientInformation.FromJson(message.Metadata!);

                return clientInformation;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error when reading client information: {exception.Message}");
                return null;
            }
        }
    }
}
