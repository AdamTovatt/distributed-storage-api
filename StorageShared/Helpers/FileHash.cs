using System.Security.Cryptography;

namespace StorageShared.Helpers
{
    public static class FileHash
    {
        public static async Task<string> GetAsStringAsync(string filePath)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    string? hashString = null;

                    await Task.Run(() =>
                    {
                        hashString = md5.GetHashAsString(stream);
                    });

                    return hashString!;
                }
            }
        }
    }
}
