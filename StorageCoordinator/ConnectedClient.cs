using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StorageCoordinator
{
    public class ConnectedClient
    {
        public NetworkStream Stream { get; set; }
        public TcpClient TcpClient { get; set; }

        public ConnectedClient(TcpClient client)
        {
            Stream = client.GetStream();
            TcpClient = client;
        }

        public void Dispose()
        {
            Stream?.Close();
            Stream?.Dispose();
            TcpClient.Close();
            TcpClient?.Dispose();
        }
    }
}
