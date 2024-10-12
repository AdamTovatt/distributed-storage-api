using System.Text;

namespace AutoUpdater
{
    public class Script
    {
        private List<Command> commands = new List<Command>();

        public Script() { }

        public Script(List<Command> commands)
        {
            this.commands = commands;
        }

        public void AddCommand(Command command)
        {
            commands.Add(command);
        }

        public static Script CreateFromString(string text)
        {
            string[] parts = text.Split(';');

            if (parts.Length == 0)
                throw new InvalidDataException($"The script was empty. It should contain commands separated by ;");

            string workingDirectory = GetWorkingDirectory(parts[0]);

            Script result = new Script();

            for (int i = 1; i < parts.Length; i++)
            {
                string processedPart = parts[i].Trim();
                if (!string.IsNullOrEmpty(processedPart) && !string.IsNullOrWhiteSpace(processedPart))
                    result.AddCommand(new Command(processedPart, workingDirectory));
            }

            return result;
        }

        private static string GetWorkingDirectory(string command)
        {
            if (!command.StartsWith("cd "))
                throw new InvalidDataException("The command to set working directory must start with cd followed by a path and be first in the script file.");

            try
            {
                return command.Substring(2).Trim();
            }
            catch
            {
                throw new InvalidDataException("Error when reading the working directory commad, it should look something like \"cd somepath/subpath/subpath\"");
            }
        }

        public static Script CreateFromFile(string filePath)
        {
            if (File.Exists(filePath))
                return CreateFromString(File.ReadAllText(filePath));
            else
                throw new FileNotFoundException($"The script file at: {filePath} could not be found");
        }

        public async Task<string> RunAsync(int timeOut = 300, ICommandLogger? commandLogger = null)
        {
            List<string> outputs = new List<string>();

            foreach (Command command in commands)
            {
                if (command.FileName.ToLower() == "requirestart") // handle special require command that breaks unless the previous output starts with something special
                {
                    if (!outputs.Last().StartsWith(command.Arguments))
                    {
                        string commandOutput = $"Encountered requireStart command that required the start of last command output to be: {command.Arguments}";
                        string commandOutput2 = "requirestart command condition was not met, breaking";

                        if (commandLogger != null)
                        {
                            commandLogger.Log(commandOutput);
                            commandLogger.Log(commandOutput2);
                        }

                        outputs.Add(commandOutput);
                        outputs.Add(commandOutput2);

                        break;
                    }
                }
                else if (command.FileName.ToLower() == "contains")
                {
                    if(!outputs.Last().Contains(command.Arguments))
                    {
                        string commandOutput = $"Encountered contains command that required the last command output to contain: {command.Arguments}";
                        string commandOutput2 = "contains command condition was not met, breaking";

                        if (commandLogger != null)
                        {
                            commandLogger.Log(commandOutput);
                            commandLogger.Log(commandOutput2);
                        }

                        outputs.Add(commandOutput);
                        outputs.Add(commandOutput2);

                        break;
                    }
                }
                else
                {
                    string commandOutput = await command.RunAsync(timeOut);
                    outputs.Add(commandOutput);

                    if (commandLogger != null)
                        commandLogger.Log(commandOutput);
                }
            }


            StringBuilder result = new StringBuilder();

            foreach (string output in outputs)
                result.AppendLine(output);

            return result.ToString();
        }
    }
}
