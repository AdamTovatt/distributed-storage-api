namespace FileByteScanner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("File Byte Scanner");
            Run();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        public static void Run()
        {
            Console.WriteLine("Input file path:");

            string filePath = Console.ReadLine()!;

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            Console.WriteLine("Input bytes to search for:");
            string bytesToSearchFor = Console.ReadLine()!;

            List<byte> bytes = new List<byte>();
            foreach (string byteString in bytesToSearchFor.Split(' '))
            {
                if (byte.TryParse(byteString.Trim(), out byte byteValue))
                    bytes.Add(byteValue);
                else
                {
                    Console.WriteLine($"Invalid byte: {byteString}");
                    return;
                }
            }

            using (FileStream file = File.OpenRead(filePath))
            {
                int matchResult = SearchBytesInFileByteByByte(file, bytes.ToArray());
                if (matchResult != -1)
                {
                    Console.WriteLine("Byte sequence found in the file!");
                    Console.WriteLine($"Match starting at index: {matchResult} in the file");
                }
                else
                {
                    Console.WriteLine("Byte sequence not found in the file.");
                }
            }
        }

        private static int SearchBytesInFileByteByByte(FileStream file, byte[] byteSequence)
        {
            int matchedIndex = 0; // Tracks how many bytes of the sequence have been matched
            int nextByte;
            int currentIndex = 0;
            int matchStartIndex = 0;

            while ((nextByte = file.ReadByte()) != -1)
            {
                if (nextByte == byteSequence[matchedIndex])
                {
                    if (matchedIndex == 0) matchStartIndex = currentIndex;

                    matchedIndex++;

                    if (matchedIndex == byteSequence.Length)
                    {
                        return matchStartIndex; // Found the sequence
                    }
                }
                else
                {
                    matchedIndex = 0; // Reset if the match breaks
                    matchStartIndex = 0;
                }

                currentIndex++;
            }

            return -1; // Sequence not found
        }
    }
}
