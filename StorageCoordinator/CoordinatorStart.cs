namespace StorageCoordinator
{
    internal class CoordinatorStart
    {
        static async Task Main(string[] args)
        {
            TcpServer server = new TcpServer(25566);
            server.Start();

            await server.AcceptClients();
        }
    }
}
