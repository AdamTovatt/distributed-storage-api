namespace AutoUpdater
{
    public class ConsoleLogger : ICommandLogger
    {
        public bool LoggingEnabled { get; set; }

        public ConsoleLogger(bool enabled)
        {
            LoggingEnabled = enabled;
        }

        public void Log(string output)
        {
            Console.WriteLine($"{DateTime.Now}: {output}");
        }
    }
}
