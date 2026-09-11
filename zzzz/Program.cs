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
        
        SplitIntoBlocks(fileBytes, 4);
    }
    
    private static byte[][] SplitIntoBlocks(byte[] bytes, int blockSize)
    {
        // Add padding first (so splitting is easier later)
        int totalBlocks = (int)Math.Ceiling(bytes.Length / (double)blockSize);
        
        Console.WriteLine($"\nLength of input file: {bytes.Length} bytes");
        Console.WriteLine($"Block size: {blockSize} bytes");
        Console.WriteLine($"{bytes.Length} / {blockSize} = {bytes.Length / blockSize} | Remainder: {bytes.Length % blockSize}\n");
        Console.WriteLine($"Input file consisting of {bytes.Length} bytes will be split into {totalBlocks} {blockSize}-byte blocks.");
        
        int requiredPadding = bytes.Length % blockSize;
        if (requiredPadding != 0)
        {
            Console.WriteLine($"Because the number of bytes isn't a perfect multiple of the block size, we need {requiredPadding} additional bytes of padding.");
        }
        else
        {
            totalBlocks++;
            requiredPadding = blockSize;
            Console.WriteLine($"Since the number of bytes is a perfect multiple of the block size, we need one additional block ({blockSize} bytes) of padding only -> {totalBlocks} blocks.");
        }
        
        byte[] paddedBytes = new byte[bytes.Length + requiredPadding];
        Array.Copy(bytes, 0, paddedBytes, 0, bytes.Length);
        
        for (int i = 0; i < requiredPadding; i++)
        {
            paddedBytes[bytes.Length + i] = (byte)requiredPadding;
        }

        foreach (var singleByte in paddedBytes)
        {
            Console.WriteLine(singleByte);
        }
        
        
        byte[][] blocks = new byte[totalBlocks][];
        return blocks;
        
        
        
        
        int copiedBytes = 0;
        for (int copiedBlocks = 0; copiedBlocks < totalBlocks; copiedBlocks++)
        {
            Array.Copy(bytes, copiedBytes, blocks[copiedBlocks], 0, blockSize);
            copiedBytes += blockSize;
        }

        foreach (var block in blocks)
        {
            Console.WriteLine($"--------------------");
            foreach (var singleByte in block)
            {
                Console.WriteLine(singleByte);
            }
            Console.WriteLine();
        }
        
        // Padding
        return blocks;
    }

    private static void Encrypt()
    {
        // 1 char -> 8 Bit -> 32 / 8 = 4 characters per block
        Console.WriteLine("Step 1: The string will be split into multiple 32-bit blocks");
    }
}