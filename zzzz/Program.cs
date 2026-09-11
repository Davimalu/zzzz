using System.Text;
using zzzz.Model;

namespace zzzz;

class Program
{
    // S-Box (4 Bit -> 4 Bit, Bijective)
    // Taken from PRESENT Cipher: https://link.springer.com/chapter/10.1007/978-3-540-74735-2_31 | Page 4
    private static readonly int[] SBox =
    {
        0xC, 0x5, 0x6, 0xB, 0x9, 0x0, 0xA, 0xD,
        0x3, 0xE, 0xF, 0x8, 0x4, 0x7, 0x1, 0x2
    };
    private static readonly int[] SBoxInv = InverseBox(SBox);
    
    // P-Box (32 Bit, same as block length | Each P-Box will be fed by 8 S-Boxes (8 * 4 = 32 Bit))
    // 32-Bit is quite small -> I didn't find an existing one -> Used "Spaltentransposition" with key length of 4
    /*  0   1   2   3
     *  4   5   6   7
     *  8   9   10  11
     *  12  13  14  15
     *  16  17  18  19
     *  20  21  22  23
     *  24  25  26  27
     *  28  29  30  31
     */
    private static readonly int[] PBox =
    {
        0, 4, 8, 12, 16, 20, 24, 28, 1, 5, 9, 13, 17, 21, 25, 29,
        2, 6, 10, 14, 18, 22, 26, 30, 3, 7, 11, 15, 19, 23, 27, 31
    };
    private static readonly int[] PBoxInv = InverseBox(PBox);
    
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
        
        // TODO: Check if ciphertext is multiple of block length
        var concatenatedBytes = ConcatenateBlocks(blocks);
        var unpaddedBytes = RemovePadding(concatenatedBytes, 4);
        File.WriteAllBytes("output.txt", unpaddedBytes);
    }

    private static byte[] ConcatenateBlocks(byte[][] blocks)
    {
        byte[] bytes = new byte[blocks.Length * blocks[0].Length];

        int writtenBytes = 0;
        foreach (var block in blocks)
        {
            Array.Copy(block, 0, bytes, writtenBytes, block.Length);
            writtenBytes += block.Length;
        }
        
        return bytes;
    }

    private static byte[] RemovePadding(byte[] bytes, int blockSize)
    {
        var paddingBytes = (int)bytes.Last();

        if (paddingBytes < 1 || paddingBytes > blockSize)
        {
            Console.WriteLine($"FATAL: Invalid number of padding bytes ({paddingBytes}) for a block size of {blockSize}.");
            throw new InvalidOperationException("Invalid number of padding bytes.");
        }
        
        for (int i = 0;  i < paddingBytes; i++)
        {
            if (bytes[bytes.Length - 1 - i] != paddingBytes)
            {
                Console.WriteLine($"FATAL: Invalid padding: Expected '{(int)paddingBytes}', got '{(int)bytes[bytes.Length - 1 - i]}'.");
                throw new InvalidOperationException("Invalid padding bytes.");
            }
        }

        byte[] unpaddedBytes = new byte[bytes.Length - paddingBytes];
        Array.Copy(bytes, 0, unpaddedBytes, 0, unpaddedBytes.Length);
        
        return unpaddedBytes;
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

    private static int[] InverseBox(int[] box)
    {
        var inverse = new int[box.Length];
        for (int i = 0; i < box.Length; i++)
        {
            inverse[box[i]] = i;
        }
        return inverse;
    }
    
    private byte[] EncryptBlock(byte[] block)
    {
        return block;
    }

    private byte[] DecryptBlock(byte[] block)
    {
        return block;
    }

    private byte[] EncryptEcb(byte[] plaintext)
    {
        return plaintext;
    }

    private byte[] DecryptEcb(byte[] ciphertext)
    {
        return ciphertext;
    }
}