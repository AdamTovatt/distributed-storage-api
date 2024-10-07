using StorageShared.Models;
using System.Net.Sockets;

namespace StorageCoordinator
{
    public class ConnectedClient
    {
        public ClientInformation ClientInformation { get; set; }
        public NetworkStream Stream { get; set; }
        public TcpClient TcpClient { get; set; }

        public ConnectedClient(TcpClient client, NetworkStream stream, ClientInformation clientInformation)
        {
            Stream = stream;
            TcpClient = client;
            ClientInformation = clientInformation;
        }

        public void Dispose()
        {
            Stream?.Close();
            Stream?.Dispose();
            TcpClient.Close();
            TcpClient?.Dispose();
        }

        public async Task SendMessageAsync(Message message)
        {
            await message.WriteMessageAsync(Stream);
        }
    }
}
