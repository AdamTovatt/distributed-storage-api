using StorageCoordinator;
using System.Net;
using System.Net.Sockets;

namespace DistributedStorageApi
{
    public class Program
    {
        private static DistributedStorage? distributedStorage;

        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            WebApplication app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.MapControllers();

            SetupDistributedStorage();

            app.Run();
        }

        private static void SetupDistributedStorage()
        {
            string? storageServerPortVariable = Environment.GetEnvironmentVariable("STORAGE_SERVER_PORT");
            int storageServerPort = storageServerPortVariable != null ? int.Parse(storageServerPortVariable) : 25566; // default to 25566 if no port is set in env variables

            StorageServer storageServer = new StorageServer(storageServerPort);
            storageServer.StartAcceptingConnections();

            Thread storageServerThread = storageServer.CreateClientAcceptingThread();
            storageServerThread.Start();

            distributedStorage = new DistributedStorage(storageServer);
        }
    }
}
