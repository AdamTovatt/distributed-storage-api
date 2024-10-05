namespace StorageShared.Helpers
{
    public class Logger
    {
        public bool Enabled { get; set; }

        public Logger(bool enabled = true)
        {
            Enabled = enabled;
        }

        public void Log(string message)
        {
            if (Enabled)
            {
                Console.WriteLine(message);
            }
        }
    }
}
