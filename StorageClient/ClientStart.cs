using StorageShared.Models;
using System.Text;

namespace StorageClient
{
    internal class ClientStart
    {
        static async Task Main(string[] args)
        {
            StorageClient client = new StorageClient("localhost", 25566);
            await client.StartAsync();

            while (true)
            {
                byte[] content = Encoding.UTF8.GetBytes(Console.ReadLine()!);

                await client.SendMessageAsync(new Message(MessageType.Utf8Encoded, content));
            }
        }
    }
}
