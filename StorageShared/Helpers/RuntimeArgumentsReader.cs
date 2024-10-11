namespace StorageShared.Helpers
{
    public class RuntimeArgumentsReader
    {
        /// <summary>
        /// Creates a dictionary from the given arguments array. The format for the run time arguments string should be "--key-1 value-1 --key-2 value-2"
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Dictionary<string, string> CreateDictionary(string[] args)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            // Iterate through the arguments array
            for (int i = 0; i < args.Length; i++)
            {
                // Check if the argument starts with "--"
                if (args[i].StartsWith("--"))
                {
                    // Extract the key by removing the "--" prefix
                    string key = args[i].Substring(2);

                    // Ensure that the next argument is the value
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        // Add the key-value pair to the dictionary
                        dictionary[key] = args[i + 1];
                        i++; // Move to the next argument (skip the value part)
                    }
                    else
                    {
                        // If the value is missing, throw an exception or handle accordingly
                        throw new ArgumentException($"Expected a value for parameter '{args[i]}'");
                    }
                }
            }

            return dictionary;
        }
    }
}
