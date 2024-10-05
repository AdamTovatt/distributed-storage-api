
using System.Net;
using System.Net.Sockets;

namespace DistributedStorageApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            /*
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseWebSockets();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();*/

            TcpListener tcp = new TcpListener(IPAddress.Any, 25566);
            tcp.Start();

            List<TcpClient> clients = new List<TcpClient>();

            while (true)
            {
                TcpClient client = await tcp.AcceptTcpClientAsync();
                clients.Add(client);
                Console.WriteLine($"Clients: {clients.Count}");
            }
        }
    }
}
