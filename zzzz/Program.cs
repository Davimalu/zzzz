using System.Collections;
using System.CommandLine;
using System.Text;
using zzzz.Model;

namespace zzzz;

class Program
{
    public const int BlockSize = 4;
    public static bool Verbose = false;
    public static bool Debug = false;
    
    // S-Box (4 Bit -> 4 Bit, Bijective)
    // Taken from PRESENT Cipher: https://link.springer.com/chapter/10.1007/978-3-540-74735-2_31 | Page 4
    public static readonly int[] SBox =
    {
        0xC, 0x5, 0x6, 0xB, 0x9, 0x0, 0xA, 0xD,
        0x3, 0xE, 0xF, 0x8, 0x4, 0x7, 0x1, 0x2
    };
    public static readonly int[] SBoxInv = InverseBox(SBox);
    
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
    public static readonly int[] PBox =
    {
        0, 4, 8, 12, 16, 20, 24, 28, 1, 5, 9, 13, 17, 21, 25, 29,
        2, 6, 10, 14, 18, 22, 26, 30, 3, 7, 11, 15, 19, 23, 27, 31
    };
    public static readonly int[] PBoxInv = InverseBox(PBox);
    
    static void Main(string[] args)
    {
        string logo = File.ReadAllText("assets/logo.txt");
        Console.WriteLine(logo);
        Console.WriteLine("Welcome to Zeugner's Zuper Zecure Zipher!\n");
        
        RootCommand rootCommand = new("A primitive (insecure!) block cipher that encrypts/decrypts any given file using a Substitution–permutation network (SPN)!");
        Option<string> inputFileOption = new("--input", "-i")
        {
            Description = "file containing the plaintext or ciphertext to encrypt/decrypt",
            Required = true,
        };
        rootCommand.Options.Add(inputFileOption);
        
        Option<string> keyFileOption = new("--key", "-k")
        {
            Description = "file containing the key to use for encryption/decryption",
            Required = true,
        };
        rootCommand.Options.Add(keyFileOption);
        
        Option<bool> encryptOption = new("--encrypt", "-e")
        {
            Description = "use the provided --key to encrypt the contents of the --input file"
        };
        rootCommand.Options.Add(encryptOption);
        
        Option<bool> decryptOption = new("--decrypt", "-d")
        {
            Description = "use the provided --key to decrypt the contents of the --input file"
        };
        rootCommand.Options.Add(decryptOption);
        
        Option<bool> verboseOption = new("--verbose", "-v")
        {
            Description = "output the state between each cipher step (Block-Splitting, Padding, S-Box, P-Box, key addition) for demonstration purposes"
        };
        rootCommand.Options.Add(verboseOption);
        
        Option<bool> debugOption = new("--debug", "-g")
        {
            Description = "pause the application between each cipher step (Block-Splitting, Padding, S-Box, P-Box, key addition) for debugging purposes"
        };
        rootCommand.Options.Add(debugOption);
        
        rootCommand.SetAction(parseResult =>
        {
            string filePath = parseResult.GetValue(inputFileOption)!;
            string keyPath = parseResult.GetValue(keyFileOption)!;
            bool encryptRequested = parseResult.GetValue(encryptOption);
            bool decryptRequested = parseResult.GetValue(decryptOption);

            if ((encryptRequested && decryptRequested) || (!encryptRequested && !decryptRequested))
            {
                throw new ArgumentException("You must specify whether you want to --encrypt or --decrypt the provided file.");
            }
            
            Mode mode = parseResult.GetValue(encryptOption) == true ? Mode.encrypt : Mode.decrypt;
            Verbose = parseResult.GetValue(verboseOption);
            Debug = parseResult.GetValue(debugOption);
            
            Console.WriteLine($"Loading {filePath}...");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File '{filePath}' does not exist!");
            }
            byte[] fileBytes = File.ReadAllBytes(filePath);
        
            Console.WriteLine($"Loading {keyPath}...");
            if (!File.Exists(keyPath))
            {
                throw new FileNotFoundException($"Key '{keyPath}' does not exist!");
            }
            byte[] keyBytes = File.ReadAllBytes(keyPath);
        
            Console.WriteLine($"\nZeugner's Zuper Zecure Zipher will {mode.ToString()} file '{filePath}' using key '{keyPath}'!");
            if (Debug) WaitForEnter();
            
            switch (mode)
            {
                case Mode.encrypt:
                {
                    var encryptedBytes = EncryptEcb(fileBytes, keyBytes);
                    File.WriteAllBytes(filePath + ".enc", encryptedBytes);
                    break;
                }
                case Mode.decrypt:
                {
                    if (fileBytes.Length % BlockSize != 0 || fileBytes.Length == 0)
                    {
                        throw new InvalidOperationException($"Ciphertext of length {fileBytes.Length} is either empty or not a multiple of BlockSize {BlockSize}");
                    }
                
                    var decryptedBytes = DecryptEcb(fileBytes, keyBytes);

                    string fileName = filePath.EndsWith(".enc") ? filePath[..^4] : filePath + ".dec";
                    File.WriteAllBytes(fileName, decryptedBytes);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        });
        
        rootCommand.Parse(args).Invoke();
    }

    public static byte[] ConcatenateBlocks(byte[][] blocks)
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

    public static byte[] RemovePadding(byte[] bytes, int blockSize)
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
    
    public static byte[] AddPadding(byte[] bytes, int blockSize)
    {
        if (Verbose)
        {
            Console.WriteLine($"\nLength of input file: {bytes.Length} bytes");
            Console.WriteLine($"Block size: {blockSize} bytes");
            Console.WriteLine($"{bytes.Length} / {blockSize} = {bytes.Length / blockSize} | Remainder: {bytes.Length % blockSize}\n");
        }
        
        int requiredPadding = blockSize - (bytes.Length % blockSize);
        if (bytes.Length % blockSize != 0)
        {
            if (Verbose) Console.WriteLine($"Because the number of bytes isn't a perfect multiple of the block size, we need {requiredPadding} additional bytes of padding.");
        }
        else
        {
            requiredPadding = blockSize;
            if (Verbose) Console.WriteLine($"Since the number of bytes is a perfect multiple of the block size, we need one additional block ({blockSize} bytes) of padding only.");
        }
        
        byte[] paddedBytes = new byte[bytes.Length + requiredPadding];
        Array.Copy(bytes, 0, paddedBytes, 0, bytes.Length);
        
        for (int i = 0; i < requiredPadding; i++)
        {
            paddedBytes[bytes.Length + i] = (byte)requiredPadding;
        }

        return paddedBytes;
    }
    
    public static byte[][] SplitIntoBlocks(byte[] bytes, int blockSize)
    {
        int totalBlocks = (int)Math.Ceiling(bytes.Length / (double)blockSize);
        if (Verbose) Console.WriteLine($"\nInput file consisting of {bytes.Length} bytes will be split into {totalBlocks} {blockSize}-byte blocks.");
        
        byte[][] blocks = new byte[totalBlocks][];

        for (int i = 0; i < totalBlocks; i++)
        {
            blocks[i] = new byte[blockSize];
            Array.Copy(bytes, i * blockSize, blocks[i], 0, blockSize);
        }
        
        return blocks;
    }
    
    public static int[] InverseBox(int[] box)
    {
        var inverse = new int[box.Length];
        for (int i = 0; i < box.Length; i++)
        {
            inverse[box[i]] = i;
        }
        return inverse;
    }

    public static byte[] SubstituteBlock(byte[] block, int[] sBox)
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

    public static byte[] PermutateBlock(byte[] block, int[] pBox)
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

    public static byte[] ApplyKeyToBlock(byte[] block, byte[] key)
    {
        byte[] processedBlock = new byte[block.Length];
        
        // XOR every bit in the block with every bit of the round key
        for (int i = 0; i < block.Length; i++)
        {
            processedBlock[i] = (byte)(block[i] ^ key[i]);
        }
        
        return processedBlock;
    }
    
    public static byte[][] GetRoundKeys(byte[] key, int rounds)
    {
        var roundKeys = new byte[rounds][];
        var keyHash = System.Security.Cryptography.SHA256.HashData(key);

        for (int i = 0; i < rounds; i++)
        {
            // Each block is 4 byte (32 Bit) -> each key should be 4 byte (32 Bit)
            roundKeys[i] = new byte[BlockSize];
            Array.Copy(keyHash, i * BlockSize, roundKeys[i], 0, BlockSize);
        }

        PrintRoundKeys(key, keyHash, roundKeys);
        return roundKeys;
    }
    
    public static byte[] EncryptBlock(byte[] block, byte[] key, bool verbose)
    {
        var substitutedBlock = SubstituteBlock(block, SBox);
        if (verbose) PrintSubstitution(block, substitutedBlock);
        
        var permutatedBlock = PermutateBlock(substitutedBlock, PBox);
        if (verbose) PrintBits("\nPermutation (Original):\n", substitutedBlock, true);
        if (verbose) PrintBits("Result:\n", permutatedBlock, false);
        
        if (verbose) PrintBits("\nKey   : ", key, false);
        if (verbose) PrintBits("Block : ", permutatedBlock, false);
        var finishedBlock = ApplyKeyToBlock(permutatedBlock, key);
        if (verbose) PrintBits("Result: ", finishedBlock, false);
        
        return finishedBlock;
    }

    public static byte[] DecryptBlock(byte[] block, byte[] key, bool verbose)
    {
        // Exact reverse of encryption
        if (verbose) PrintBits("\nKey   : ", key, false);
        if (verbose) PrintBits("Block : ", block, false);
        var keyedBlock = ApplyKeyToBlock(block, key);
        if (verbose) PrintBits("Result: ", keyedBlock, false);
        
        var unpermutatedBlock = PermutateBlock(keyedBlock, PBoxInv);
        if (verbose) PrintBits("\nPermutation (Original):\n", keyedBlock, false);
        if (verbose) PrintBits("Result:\n", unpermutatedBlock, true);
        
        var unsubstitutedBlock = SubstituteBlock(unpermutatedBlock, SBoxInv);
        if (verbose) PrintSubstitution(unpermutatedBlock, unsubstitutedBlock);
        
        return unsubstitutedBlock;
    }

    public static byte[] EncryptEcb(byte[] fileBytes, byte[] keyBytes)
    {
        var paddedBytes = AddPadding(fileBytes, BlockSize);
        var blocks = SplitIntoBlocks(paddedBytes, BlockSize);
        PrintBlocks(blocks);

        var roundKeys = GetRoundKeys(keyBytes, 8);
        
        // Multiple Rounds, ECB Mode
        byte[][] encryptedBlocks = new byte[blocks.Length][];
        for (int i = 0; i < 8; i++)
        {
            if (Verbose)
                Console.WriteLine($"--- Round {i+1} ---");
            
            for (int j = 0; j < blocks.Length; j++)
            {
                encryptedBlocks[j] = EncryptBlock(blocks[j], roundKeys[i], i == 0);
            }
            
            blocks = encryptedBlocks;
            PrintBlocks(encryptedBlocks);
        }
        
        return ConcatenateBlocks(encryptedBlocks);
    }

    public static byte[] DecryptEcb(byte[] fileBytes, byte[] keyBytes)
    {
        var blocks = SplitIntoBlocks(fileBytes, BlockSize);
        PrintBlocks(blocks);

        var roundKeys = GetRoundKeys(keyBytes, 8);
        
        // Rounds
        byte[][] decryptedBlocks = new byte[blocks.Length][];
        for (int i = 7; i >= 0; i--)
        {
            if (Verbose)
                Console.WriteLine($"--- Round {i+1} ---");
            
            for (int j = 0; j < blocks.Length; j++)
            {
                decryptedBlocks[j] = DecryptBlock(blocks[j], roundKeys[i], i == 7);
            }

            blocks = decryptedBlocks;
            PrintBlocks(decryptedBlocks);
        }
        
        var concatenatedBytes = ConcatenateBlocks(blocks);
        var unpaddedBytes = RemovePadding(concatenatedBytes, BlockSize);
        
        return unpaddedBytes;
    }
    
    private static void PrintRoundKeys(byte[] key, byte[] keyHash, byte[][] roundKeys)
    {
        if (!Verbose)
            return;
        
        Console.WriteLine($"Key: {Encoding.ASCII.GetString(key)}");
        Console.WriteLine($"Key Hash: {Convert.ToHexString(keyHash)}");
        Console.WriteLine();

        for (int i = 0; i < roundKeys.Length; i++)
        {
            Console.WriteLine($"K{i}: {Convert.ToHexString(roundKeys[i])}");
        }
        
        if (Debug) WaitForEnter();
    }
    
    public static void PrintBits(string prefix, byte[] block, bool addNewLines)
    {
        if(!Verbose) return;
        
        Console.Write(prefix);
        
        foreach (byte b in block)
        {
            Console.Write(Convert.ToString(b, 2).PadLeft(8, '0') + " ");
            if (addNewLines)
            {
                Console.WriteLine();
            }
        }
        
        Console.WriteLine();
        
        if (Debug) WaitForEnter();
    }
    
    // This method was written with the help of generative AI (GLM 5.3)
    public static void PrintBlocks(byte[][] blocks, int blocksPerLine = 5)
    {
        if (!Verbose)
            return;
        
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
        
        if (Debug) WaitForEnter();
    }

    public static void PrintBlock(string prefix, byte[] block)
    {
        Console.Write(prefix);
        foreach (var singleByte in block)
        {
            Console.WriteLine($"{singleByte:X2}");
        }
        
        if (Debug) WaitForEnter();
    }
    
    private static void PrintSubstitution(byte[] block, byte[] substitutedBlock)
    {
        if (!Verbose)
            return;
        
        for (int i = 0; i < block.Length; i++)
        {
            Console.WriteLine($"{block[i]:X2} -> {substitutedBlock[i]:X2}");
        }
        
        if (Debug) WaitForEnter();
    }
    
    public static void WaitForEnter()
    {
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.Enter) { }
    }
}