using System.Diagnostics;
using System.Text;

namespace AutoUpdater
{
    public class Command
    {
        public string WorkingDirectory { get; set; }
        public string FileName { get; set; }
        public string Arguments { get; set; }

        public Command(string workingDirectory, string fileName, string arguments)
        {
            WorkingDirectory = workingDirectory;
            FileName = fileName;
            Arguments = arguments;
        }

        public Command(string command, string workingDirectory)
        {
            StringBuilder argument = new StringBuilder();
            string[] parts = command.Split();

            if (parts.Length > 1)
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    argument.Append(string.Format("{0} ", parts[i]));
                }
            }

            WorkingDirectory = workingDirectory;
            FileName = parts[0];
            Arguments = argument.ToString().Trim();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", FileName, Arguments);
        }

        public async Task<string> RunAsync(int timeOut = 300)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WorkingDirectory = WorkingDirectory,
                FileName = FileName,
                Arguments = Arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                // Start the process
                process.Start();

                // Create a cancellation token that will cancel after the timeout period
                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    Task delayTask = Task.Delay(timeOut * 1000, cts.Token);
                    Task processTask = process.WaitForExitAsync();

                    // Wait for either the process to finish or the timeout
                    Task finishedTask = await Task.WhenAny(processTask, delayTask);

                    if (finishedTask == delayTask)
                    {
                        // Timeout reached, kill the process and throw exception or return message
                        process.Kill();
                        return "Process was killed due to timeout.";
                    }

                    // Process completed within the timeout, cancel the delay task
                    cts.Cancel();

                    // Read the process output after it exits
                    return await process.StandardOutput.ReadToEndAsync();
                }
            }
        }
    }
}
