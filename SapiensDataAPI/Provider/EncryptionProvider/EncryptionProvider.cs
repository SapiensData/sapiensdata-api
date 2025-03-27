using SapiensDataAPI.Services.GlobalVariable;
using SoftFluent.EntityFrameworkCore.DataEncryption;
using System.Security.Cryptography;
using System.Text;

namespace SapiensDataAPI.Provider.EncryptionProvider
{
	public class EncryptionProvider(GlobalVariableService globalVariableService) : IEncryptionProvider
	{
		private readonly GlobalVariableService _globalVariableService = globalVariableService;

		private readonly int _nonceSize = AesGcm.NonceByteSizes.MaxSize;
		private readonly int _tagSize = AesGcm.TagByteSizes.MaxSize;

		// TODO maybe in the future include "associated data" in the encryption process

		public byte[] Decrypt(byte[] input)
		{
			if (input.Length == 0)
			{
				return [];
			}

			if (input.Length < _nonceSize + _tagSize)
			{
				throw new ArgumentException("Input is too short.");
			}

			string encryptionKey = _globalVariableService.SymmetricKey;
			byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
			if (key.Length != 32)
			{
				throw new ArgumentException("Encryption key must be 32 bytes.");
			}

			byte[] nonce = new byte[_nonceSize];
			byte[] tag = new byte[_tagSize];
			int ciphertextLength = input.Length - nonce.Length - tag.Length;
			byte[] ciphertext = new byte[ciphertextLength];

			try
			{
				Buffer.BlockCopy(input, 0, nonce, 0, nonce.Length);
				Buffer.BlockCopy(input, nonce.Length, tag, 0, tag.Length);
				Buffer.BlockCopy(input, nonce.Length + tag.Length, ciphertext, 0, ciphertextLength);
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Input is wrong.", ex);
			}

			byte[] plaintext = new byte[ciphertextLength];
			try
			{
				using AesGcm aesGcm = new(key, tag.Length);
				aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
			}
			catch (Exception e)
			{
				throw new CryptographicException($"Something went wrong while decrypting: {e.Message}", e);
			}

			return plaintext;
		}

		public byte[] Encrypt(byte[] input)
		{
			if (input.Length == 0)
			{
				return [];
			}

			string encryptionKey = _globalVariableService.SymmetricKey;
			byte[] key = Encoding.UTF8.GetBytes(encryptionKey);
			if (key.Length != 32)
			{
				throw new ArgumentException("Encryption key must be 32 bytes.");
			}

			byte[] nonce = new byte[_nonceSize];
			RandomNumberGenerator.Fill(nonce);

			byte[] ciphertext = new byte[input.Length];
			byte[] tag = new byte[_tagSize];
			byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];

			try
			{
				using AesGcm aesGcm = new(key, tag.Length);
				aesGcm.Encrypt(nonce, input, ciphertext, tag);
			}
			catch (Exception e)
			{
				throw new CryptographicException($"Something went wrong while encrypting: {e.Message}", e);
			}

			try
			{
				Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
				Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
				Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Input is wrong.", ex);
			}

			return result;
		}
	}
}