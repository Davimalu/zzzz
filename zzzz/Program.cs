using System.Net.Security;
using static System.Int32;

namespace zzzz;

class Program
{
    static void Main(string[] args)
    {
        File.ReadAllText("assets/logo.txt");
        
        Console.WriteLine("Welcome to Zeugner's Zuper Zecure Zypher!");
        Console.WriteLine("Please select your desired mode of operation:");
        Console.WriteLine("1) Encryption");
        Console.WriteLine("2) Decryption");
        
        var input = Console.ReadLine();
        if (!TryParse(input, out var selection))
        {
            Console.WriteLine("Invalid input. Please enter a valid number!");
            return;
        }

        switch (selection)
        {
            case 1:
                Console.WriteLine("Encryption");
                Encrypt();
                break;
            case 2:
                Console.WriteLine("Decryption");
                break;
            default:
                Console.WriteLine("Invalid input. Please enter either 1) for encryption or 2) for decryption!");
                break;
        }
    }

    private static void Encrypt()
    {
        Console.WriteLine("Please enter the key to use for encryption:");
        string? key = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(key))
        {
            Console.WriteLine("Please enter a valid key!");
            return;
        }
        
        Console.WriteLine("Please enter the text to encrypt:");
        string? cleartext = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(cleartext))
        {
            Console.WriteLine("Please enter a valid text!");
            return;
        }
        
        Console.WriteLine($"Zeugner's Zuper Zecure Zipher will encrypt '{cleartext}' using key '{key}'!");
        
        // 1 char -> 8 Bit -> 32 / 8 = 4 characters per block
        Console.WriteLine("Step 1: The string will be split into multiple 32-bit blocks");
    }
}