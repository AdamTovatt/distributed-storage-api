using StorageCoordinator;

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

            // Register the distributed storage service
            builder.Services.AddHostedService<DistributedStorageService>();  // Register distributed storage as a background service

            WebApplication app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.MapControllers();

            //SetupDistributedStorage();

            app.Run();
        }
    }

    public class DistributedStorageService : BackgroundService
    {
        private StorageServer? _storageServer;
        private DistributedStorage? _distributedStorage;

        public DistributedStorageService() { }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string? storageServerPortVariable = Environment.GetEnvironmentVariable("STORAGE_SERVER_PORT");
            int storageServerPort = storageServerPortVariable != null ? int.Parse(storageServerPortVariable) : 25566;

            string? loggingEnabledVariable = Environment.GetEnvironmentVariable("LOG");
            bool loggingEnabled = loggingEnabledVariable != null ? bool.Parse(loggingEnabledVariable) : true;

            StorageShared.Helpers.Logger logger = new StorageShared.Helpers.Logger(loggingEnabled);
            _storageServer = new StorageServer(storageServerPort, logger);
            _storageServer.StartAcceptingConnections();
            _storageServer.StartClientAcceptingThread(stoppingToken);

            _distributedStorage = DistributedStorage.Initialize(_storageServer, 5120);

            logger.Log($"Distributed Storage Service started on port {storageServerPort}.");

            // Wait until the application is stopping
            stoppingToken.Register(OnStopping);

            return Task.CompletedTask;
        }

        private void OnStopping()
        {
            // Gracefully stop accepting new connections and dispose of resources
            _storageServer?.StopAcceptingConnections();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
        }
    }
}