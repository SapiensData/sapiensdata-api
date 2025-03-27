using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace SapiensDataAPI.Provider.Argon2IdPasswordHasher
{
	public class Argon2IdPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
	{
		private const int DefaultDegreeOfParallelism = 4;
		private const int DefaultMemorySize = 12288;
		private const int DefaultIterations = 4;
		private const int KeySize = 32;
		private readonly byte[] _knownSecret = Encoding.UTF8.GetBytes("IDontKnowWhatImDoing");

		public string HashPassword(TUser user, string password)
		{
			byte[] salt = new byte[16];
			using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(salt);
			}

			string userId = (user as IdentityUser)?.Id ?? "";
			byte[] associatedData = Encoding.UTF8.GetBytes(userId);

			// Prepare password bytes and initialize Argon2id with default parameters
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
			Argon2id argon2Id = new(passwordBytes)
			{
				DegreeOfParallelism = DefaultDegreeOfParallelism,
				MemorySize = DefaultMemorySize,
				Iterations = DefaultIterations,
				Salt = salt,
				AssociatedData = associatedData,
				KnownSecret = _knownSecret
			};

			// Compute the hash
			byte[] hash = argon2Id.GetBytes(KeySize);

			// Format: iterations:memorySize:degreeOfParallelism:salt:hash
			string formattedHash = string.Join(":",
				DefaultIterations,
				DefaultMemorySize,
				DefaultDegreeOfParallelism,
				Convert.ToBase64String(salt),
				Convert.ToBase64String(hash));

			return formattedHash;
		}

		public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
		{
			// Expected format: iterations:memorySize:degreeOfParallelism:salt:hash
			string[] parts = hashedPassword.Split(':');
			if (parts.Length != 5)
			{
				return PasswordVerificationResult.Failed;
			}

			// Parse stored parameters
			if (!int.TryParse(parts[0], out int iterations))
			{
				return PasswordVerificationResult.Failed;
			}

			if (!int.TryParse(parts[1], out int memorySize))
			{
				return PasswordVerificationResult.Failed;
			}

			if (!int.TryParse(parts[2], out int degreeOfParallelism))
			{
				return PasswordVerificationResult.Failed;
			}

			byte[] salt;
			byte[] expectedHash;
			try
			{
				salt = Convert.FromBase64String(parts[3]);
				expectedHash = Convert.FromBase64String(parts[4]);
			}
			catch
			{
				return PasswordVerificationResult.Failed;
			}

			// Use the user's Id as associated data (if available)
			string userId = (user as IdentityUser)?.Id ?? "";
			byte[] associatedData = Encoding.UTF8.GetBytes(userId);

			// Compute the hash using the provided parameters
			byte[] passwordBytes = Encoding.UTF8.GetBytes(providedPassword);
			Argon2id argon2Id = new(passwordBytes)
			{
				DegreeOfParallelism = degreeOfParallelism,
				MemorySize = memorySize,
				Iterations = iterations,
				Salt = salt,
				AssociatedData = associatedData,
				KnownSecret = _knownSecret
			};

			byte[] computedHash = argon2Id.GetBytes(KeySize);

			return computedHash.SequenceEqual(expectedHash)
				? PasswordVerificationResult.Success
				: PasswordVerificationResult.Failed;
		}
	}
}