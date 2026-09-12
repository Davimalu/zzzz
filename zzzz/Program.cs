using System.Collections;
using System.Text;
using zzzz.Model;

namespace zzzz;

class Program
{
    private const int BlockSize = 4;
    
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

        var paddedBytes = AddPadding(fileBytes, BlockSize);
        var blocks = SplitIntoBlocks(paddedBytes, BlockSize);
        PrintBlocks(blocks);
        
        // Rounds
        byte[][] encryptedBlocks = new byte[blocks.Length][];
        for (int i = 0; i < 8; i++)
        {
            Console.WriteLine($"Round {i+1}");
            
            for (int j = 0; j < blocks.Length; j++)
            {
                encryptedBlocks[j] = EncryptBlock(blocks[j]);
            }

            blocks = encryptedBlocks;
            PrintBlocks(encryptedBlocks);
        }
        
        // Decryption
        
        // TODO: Check if ciphertext is multiple of block length
        var concatenatedBytes = ConcatenateBlocks(blocks);
        var unpaddedBytes = RemovePadding(concatenatedBytes, BlockSize);
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
            throw new InvalidOperationException($"Invalid number of padding bytes ({paddingBytes}) for a block size of {blockSize}.");
        }
        
        for (int i = 0;  i < paddingBytes; i++)
        {
            if (bytes[bytes.Length - 1 - i] != paddingBytes)
            {
                throw new InvalidOperationException($"Invalid padding: Expected '{(int)paddingBytes}', got '{(int)bytes[bytes.Length - 1 - i]}'.");
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

    private static void PrintBlock(byte[] block)
    {
        foreach (var singleByte in block)
        {
            Console.WriteLine($"{singleByte,4:X2}");
        }
        Console.WriteLine();
    }
    
    private static void PrintBits(byte[] block, bool addNewLines)
    {
        foreach (byte b in block)
        {
            Console.Write(Convert.ToString(b, 2).PadLeft(8, '0') + " ");
            if (addNewLines)
            {
                Console.WriteLine();
            }
        }
            
        Console.WriteLine();
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

    private static byte[] SubstituteBlock(byte[] block, int[] sBox)
    {
        byte[] substitutedBlock = new byte[block.Length];
        
        // Block: 32-Bit (4 Bytes) | 1 Block will be processed by 8 S-Boxes
        for (int i = 0; i < block.Length; i++)
        {
            // 1 Byte -> 2 S-Boxes (take 4 Bit each)
            byte highNibble = (byte)(block[i] >> 4); // shift right by 4 Bit -> removes lower 4 Bit
            byte lowNibble = (byte)(block[i] & 0x0f); // 0x0F -> 0000 1111 | & -> Bitwise AND | -> Remove upper 4 Bit

            byte substitutedHighNibble = (byte)sBox[highNibble];
            byte substitutedLowNibble = (byte)sBox[lowNibble];
            
            substitutedBlock[i] = (byte)((substitutedHighNibble << 4) | substitutedLowNibble);
        }
        
        return substitutedBlock;
    }

    private static byte[] PermutateBlock(byte[] block, int[] pBox)
    {
        BitArray unpermutatedBits = new BitArray(block);
        BitArray permutatedBits = new BitArray(unpermutatedBits.Length);
        
        // C#'s BitArray numbers bits LSB-first within each byte (index 0 = least significant bit)
        // The P-Table at the top of this program is written MSB-first (leftmost bit = position 0)
        // This method translates paper indices to BitArray indices by mirroring the bit order within each byte; the byte order is unchanged.
        // This method was written with the help of generative AI (GLM 5.3)
        static int Idx(int paper)
        {
            const int bitsPerByte = 8;
            int byteNumber = paper / bitsPerByte;                   // which byte this bit belongs to
            int byteStart = bitsPerByte * byteNumber;               // first BitArray index of that byte
            int offsetFromMsb = paper % bitsPerByte;
            int offsetFromLsb = bitsPerByte - 1 - offsetFromMsb;    // mirror: MSB-first -> LSB-first

            return byteStart + offsetFromLsb;
        }

        for (int i = 0; i < unpermutatedBits.Length; i++)
        {
            permutatedBits[Idx(pBox[i])] = unpermutatedBits[Idx(i)];
        }
        
        byte[] permutatedBlock = new byte[block.Length];
        permutatedBits.CopyTo(permutatedBlock, 0);
        
        return permutatedBlock;
    }
    
    private static byte[] EncryptBlock(byte[] block)
    {
        Console.WriteLine("\n--- New Block ---\n");
        
        var substitutedBlock = SubstituteBlock(block, SBox);
        Console.WriteLine("Block after Substitution (S-Box):");
        PrintBlock(substitutedBlock);
        
        Console.WriteLine("Block before Permutation (P-Box):");
        PrintBits(substitutedBlock, addNewLines: true);
        var permutatedBlock = PermutateBlock(substitutedBlock, PBox);
        Console.WriteLine("Block after Permutation (P-Box):");
        PrintBits(permutatedBlock, addNewLines: false);
        
        return permutatedBlock;
    }

    private static byte[][] GetRoundKeys(byte[] key, int rounds)
    {
        var roundKeys = new byte[rounds][];
        var keyHash = System.Security.Cryptography.SHA256.HashData(key);

        for (int i = 0; i < rounds; i++)
        {
            // Each block is 4 byte (32 Bit) -> each key should be 4 byte (32 Bit)
            roundKeys[i] = new byte[BlockSize];
            Array.Copy(keyHash, i * BlockSize, roundKeys[i], 0, BlockSize);
        }

        return roundKeys;
    }

    private static byte[] DecryptBlock(byte[] block)
    {
        return block;
    }

    private static byte[] EncryptEcb(byte[] plaintext)
    {
        return plaintext;
    }

    private static byte[] DecryptEcb(byte[] ciphertext)
    {
        return ciphertext;
    }
}