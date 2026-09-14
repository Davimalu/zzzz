# Zeugner's Zuper Zecure Zipher
Zeugner's Zuper Zecure Zipher (zzzz) is a symmetric block cipher with a block length of 32 bits and an arbitrary key length. It implements a Substitution-Permutation Network (SPN) with the 4-bit -> 4-bit S-Box of the [PRESENT cipher](https://link.springer.com/chapter/10.1007/978-3-540-74735-2_31) and a P-Box based on column transposition, executed over 8 rounds. The round keys are derived from the SHA256 hash of the symmetric key and are XOR-combined with the output of the P-Box in every round.

> [!WARNING]
> Although the algorithm correctly implements the theoretical foundations of symmetric block ciphers, it cannot be considered secure due to its small block size, low round count, usage of ECB Mode and primitive P-Box and key derivation function. It is a toy cipher that I developed for learning purposes as part of my master's studies. This cipher should not be used to encrypt important data!

## Installation / Compilation

To compile Zeugner's Zuper Zecure Zipher, the .NET SDK version 10.0 must be installed on your system.

1. Clone the git repository
2. Change the working directory into the downloaded `zzzz` folder
3. Compile the application

```bash
git clone https://github.com/davimalu/zzzz
cd zzzz
dotnet build
```

4. The finished executable can then be found in `./bin/Debug/net10.0/zzzz`

## Usage
```text
zzzz [options]

Options:
-i, --input <input> (REQUIRED)  file containing the plaintext or ciphertext to encrypt/decrypt
-k, --key <key> (REQUIRED)      file containing the key to use for encryption/decryption
-e, --encrypt                   use the provided --key to encrypt the contents of the --input file
-d, --decrypt                   use the provided --key to decrypt the contents of the --input file
-v, --verbose                   output the state between each cipher step (Block-Splitting, Padding, S-Box, P-Box, key addition) for demonstration
purposes
-g, --debug                     pause the application between each cipher step (Block-Splitting, Padding, S-Box, P-Box, key addition) for
debugging purposes
-?, -h, --help                  Show help and usage information
--version                       Show version information
```

### Examples

Encrypt the file `plaintext.txt` using key `key.txt`:

```bash
./zzzz --input plaintext.txt --key key.txt --encrypt
```

The ciphertext will be saved in `plaintext.txt.enc`. 

---

Decrypt it again:

```bash
./zzzz --input plaintext.txt.enc --key key.txt --decrypt
```

If the input file ends with `.enc`, the cleartext will be saved in the original filename without `.enc`, e.g. `plaintext.txt`.  
If the input file does not end with `.enc`, the cleartext will be saved in `<filename>.dec`