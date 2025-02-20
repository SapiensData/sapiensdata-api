using DotNetEnv;
using SapiensDataAPI.Services.GlobalVariable;
using SoftFluent.EntityFrameworkCore.DataEncryption;
using System.Security.Cryptography;
using System.Text;

namespace SapiensDataAPI.Provider.EncryptionProvider
{
	public class EncryptionProvider(GlobalVariableService globalVariableService) : IEncryptionProvider
	{
		private readonly GlobalVariableService _globalVariableService = globalVariableService;

		public const int AesBlockSize = 128;

		public const int InitializationVectorSize = 16;

		private readonly CipherMode _mode = CipherMode.CBC;
		private readonly PaddingMode _padding = PaddingMode.PKCS7;

		private Aes CreateCryptographyProvider(CipherMode mode, PaddingMode padding)
		{
			//Env.Load(".env");

			//var encryptionKey = Env.GetString("ENCRYPTION_KEY") ?? throw new InvalidOperationException("Encryption key is not configured properly.");

			var encryptionKey = _globalVariableService.SymmetricKey;

			var encryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKey);

			if (encryptionKeyBytes.Length != 32)
			{
				throw new ArgumentException("Encryption key must be 32 bytes long.");
			}

			var aes = Aes.Create();

			aes.Mode = mode;
			aes.KeySize = encryptionKeyBytes.Length * 8;
			aes.BlockSize = AesBlockSize;
			aes.FeedbackSize = AesBlockSize;
			aes.Padding = padding;
			aes.Key = encryptionKeyBytes;
			aes.GenerateIV();

			return aes;
		}

		public byte[] Encrypt(byte[] input)
		{
			if (input == null || input.Length == 0)
			{
				return [];
			}

			// Create the AES provider without a preset IV
			using Aes aes = CreateCryptographyProvider(_mode, _padding);

			using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
			using MemoryStream memoryStream = new();
			// Write the IV at the beginning of the stream
			memoryStream.Write(aes.IV, 0, aes.IV.Length);

			using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
			{
				cryptoStream.Write(input, 0, input.Length);
				cryptoStream.FlushFinalBlock();
			}

			// Return the combined IV + ciphertext
			return memoryStream.ToArray();
		}

		public byte[] Decrypt(byte[] input)
		{
			if (input == null || input.Length == 0)
			{
				return [];
			}

			using MemoryStream memoryStream = new(input);
			// Extract the IV from the first 16 bytes
			byte[] iv = new byte[16];
			int bytesRead = memoryStream.Read(iv, 0, iv.Length);
			if (bytesRead != iv.Length)
			{
				throw new ArgumentException("Invalid input: IV missing or corrupt.");
			}

			// Create the AES provider without a preset IV and then assign the extracted IV
			using Aes aes = CreateCryptographyProvider(_mode, _padding);
			aes.IV = iv;

			using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
			using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
			using MemoryStream outputStream = new();
			cryptoStream.CopyTo(outputStream);
			return outputStream.ToArray();
		}
	}
}
