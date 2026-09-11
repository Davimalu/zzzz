using System.Text;
using zzzz.Model;

namespace zzzz;

class Program
{
    static void Main(string[] args)
    {
        string logo = File.ReadAllText("assets/logo.txt");
        Console.WriteLine(logo);
        Console.WriteLine("Welcome to Zeugner's Zuper Zecure Zypher!\n");

        if (args.Length != 3)
        {
            Console.WriteLine("Usage: zzzz <file> <key> <mode>");
            return;
        }
        
        var filePath = args[0];
        var keyPath = args[1];
        var modeString = args[2];
        
        Console.WriteLine($"Loading {filePath}...");
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File '{filePath}' does not exist!");
            return;
        }
        byte[] fileBytes = File.ReadAllBytes(filePath);
        
        Console.WriteLine($"Loading {keyPath}...");
        if (!File.Exists(keyPath))
        {
            Console.WriteLine($"Key '{keyPath}' does not exist!");
            return;
        }
        byte[] keyBytes = File.ReadAllBytes(keyPath);

        if (!Enum.TryParse<Mode>(modeString, out var mode))
        {
            Console.WriteLine($"Mode '{modeString}' does not exist!");
            return;
        }
        
        Console.WriteLine($"\nZeugner's Zuper Zecure Zypher will {mode.ToString()} file '{filePath}' using key '{keyPath}'!");

        var paddedBytes = AddPadding(fileBytes, 4);
        var blocks = SplitIntoBlocks(paddedBytes, 4);
        PrintBlocks(blocks);
    }

    private static byte[] AddPadding(byte[] bytes, int blockSize)
    {
        Console.WriteLine($"\nLength of input file: {bytes.Length} bytes");
        Console.WriteLine($"Block size: {blockSize} bytes");
        Console.WriteLine($"{bytes.Length} / {blockSize} = {bytes.Length / blockSize} | Remainder: {bytes.Length % blockSize}\n");
        
        int requiredPadding = blockSize - (bytes.Length % blockSize);
        if (bytes.Length % blockSize != 0)
        {
            Console.WriteLine($"Because the number of bytes isn't a perfect multiple of the block size, we need {requiredPadding} additional bytes of padding.");
        }
        else
        {
            requiredPadding = blockSize;
            Console.WriteLine($"Since the number of bytes is a perfect multiple of the block size, we need one additional block ({blockSize} bytes) of padding only.");
        }
        
        byte[] paddedBytes = new byte[bytes.Length + requiredPadding];
        Array.Copy(bytes, 0, paddedBytes, 0, bytes.Length);
        
        for (int i = 0; i < requiredPadding; i++)
        {
            paddedBytes[bytes.Length + i] = (byte)requiredPadding;
        }

        return paddedBytes;
    }
    
    
    private static byte[][] SplitIntoBlocks(byte[] bytes, int blockSize)
    {
        int totalBlocks = (int)Math.Ceiling(bytes.Length / (double)blockSize);
        Console.WriteLine($"\nInput file consisting of {bytes.Length} bytes will be split into {totalBlocks} {blockSize}-byte blocks.");
        
        byte[][] blocks = new byte[totalBlocks][];

        for (int i = 0; i < totalBlocks; i++)
        {
            blocks[i] = new byte[blockSize];
            Array.Copy(bytes, i * blockSize, blocks[i], 0, blockSize);
        }
        
        return blocks;
    }
    
    // This method was written with the help of generative AI (GLM 5.3)
    private static void PrintBlocks(byte[][] blocks, int blocksPerLine = 5)
    {
        Console.WriteLine();
        
        for (int start = 0; start < blocks.Length; start += blocksPerLine)
        {
            var asciiLine = new StringBuilder();
            var hexLine = new StringBuilder();

            int end = Math.Min(start + blocksPerLine, blocks.Length);

            for (int i = start; i < end; i++)
            {
                if (i > start) // block separator between blocks, not before the first
                {
                    asciiLine.Append(" |");
                    hexLine.Append(" |");
                }

                foreach (byte singleByte in blocks[i])
                {
                    // Printable ASCII range -> show the char; otherwise a dot
                    char c = (singleByte >= 32 && singleByte < 127) ? (char)singleByte : '.';
                    asciiLine.Append($"{c,4}");
                    hexLine.Append($"{singleByte,4:X2}");
                }
            }
            
            Console.WriteLine(asciiLine);
            Console.WriteLine(hexLine);
            Console.WriteLine();
        }
    }
}