using SapiensDataAPI.Services.GlobalVariable;
using SoftFluent.EntityFrameworkCore.DataEncryption;
using System.Security.Cryptography;
using System.Text;

namespace SapiensDataAPI.Provider.EncryptionProvider
{
	public class EncryptionProvider(GlobalVariableService globalVariableService) : IEncryptionProvider
	{
		private const int AesBlockSize = 128;
		private const int InitializationVectorSize = 16;
		private const CipherMode Mode = CipherMode.CBC;
		private const PaddingMode Padding = PaddingMode.PKCS7;
		private readonly GlobalVariableService _globalVariableService = globalVariableService;

		public byte[] Encrypt(byte[] input)
		{
			if (input.Length == 0)
			{
				return [];
			}

			// Create the AES provider without a preset IV
			using Aes aes = CreateCryptographyProvider(Mode, Padding);

			using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
			using MemoryStream memoryStream = new();
			// Write the IV at the beginning of the stream
			memoryStream.Write(aes.IV, 0, aes.IV.Length);

			using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write, true))
			{
				cryptoStream.Write(input, 0, input.Length);
				cryptoStream.FlushFinalBlock();
			}

			// Return the combined IV + ciphertext
			return memoryStream.ToArray();
		}

		public byte[] Decrypt(byte[] input)
		{
			switch (input.Length)
			{
				case 0:
					return [];
				case < InitializationVectorSize:
					throw new ArgumentException("Input is too short to contain a valid IV.");
			}

			using MemoryStream memoryStream = new(input);
			// Extract the IV from the first 16 bytes
			byte[] iv = new byte[InitializationVectorSize];
			int bytesRead = memoryStream.Read(iv, 0, iv.Length);
			if (bytesRead != iv.Length)
			{
				throw new ArgumentException("Invalid input: IV missing or corrupt.");
			}

			// Create the AES provider without a preset IV and then assign the extracted IV
			using Aes aes = CreateCryptographyProvider(Mode, Padding);
			aes.IV = iv;

			using ICryptoTransform decryptionModule = aes.CreateDecryptor(aes.Key, aes.IV);
			using CryptoStream cryptoStream = new(memoryStream, decryptionModule, CryptoStreamMode.Read, true);
			using MemoryStream outputStream = new();
			cryptoStream.CopyTo(outputStream);
			return outputStream.ToArray();
		}

		private Aes CreateCryptographyProvider(CipherMode mode, PaddingMode padding)
		{
			string encryptionKey = _globalVariableService.SymmetricKey;

			byte[] encryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKey);

			if (encryptionKeyBytes.Length != 32)
			{
				throw new ArgumentException("Encryption key must be 32 bytes long.");
			}

			Aes aes = Aes.Create();

			aes.Mode = mode;
			aes.KeySize = encryptionKeyBytes.Length * 8;
			aes.BlockSize = AesBlockSize;
			aes.FeedbackSize = AesBlockSize;
			aes.Padding = padding;
			aes.Key = encryptionKeyBytes;
			aes.GenerateIV();

			return aes;
		}
	}
}