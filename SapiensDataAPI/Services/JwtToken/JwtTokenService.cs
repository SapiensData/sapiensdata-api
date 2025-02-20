// SapiensDataAPI/Services/JwtTokenService.cs
using Microsoft.AspNetCore.Identity; // Import Identity for managing user roles and identity
using Microsoft.IdentityModel.Tokens; // Import for security token handling
using SapiensDataAPI.Dtos.Auth.Request; // Import request DTOs for authentication
using SapiensDataAPI.Dtos.Auth.Response; // Import response DTOs for authentication
using SapiensDataAPI.Models; // Import user model
using System.Globalization;
using System.IdentityModel.Tokens.Jwt; // Import for handling JWT tokens
using System.Security.Claims; // Import for handling claims in JWT
using System.Text;
using System.Text.Json; // Import for encoding the JWT key

namespace SapiensDataAPI.Services.JwtToken // Define the service namespace
{
	public class JwtTokenService(UserManager<ApplicationUserModel> userManager, IConfiguration configuration) : IJwtTokenService // Implement the IJwtTokenService interface
	{
		private readonly UserManager<ApplicationUserModel> _userManager = userManager; // User manager for managing user data and roles
		private readonly IConfiguration _configuration = configuration; // Configuration to access settings like JWT key, issuer, etc.

		public async Task<string> GenerateToken(ApplicationUserModel user) // Method to generate a JWT token
		{
			// Get user roles
			IList<string> roles = await _userManager.GetRolesAsync(user);

			if (string.IsNullOrEmpty(user.UserName))
			{
				throw new InvalidOperationException("No username provided.");
			}

			// Create claims list
			List<Claim> claims =
			[
				new(JwtRegisteredClaimNames.Sub, user.UserName), // Add user's username as a claim
				new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				// Add roles as claims
				.. roles.Select(role => new Claim("role", role)) // Add each user role as a claim, // Add a unique token ID
            ];

			string? jwtKey = _configuration["Jwt:Key"];

			if (string.IsNullOrEmpty(jwtKey))
			{
				throw new InvalidOperationException("JWT Key is not configured in the settings.");
			}

			SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtKey)); // Generate the symmetric security key

			SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256); // Create signing credentials using HMAC SHA256

			// Create the token
			JwtSecurityToken token = new(
				issuer: _configuration["Jwt:Issuer"], // Define the token issuer
				audience: _configuration["Jwt:Audience"], // Define the token audience
				claims: claims, // Pass the claims into the token
				DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"], CultureInfo.InvariantCulture)), // Set token expiration time
				signingCredentials: creds); // Pass signing credentials

			// Return the generated token
			return new JwtSecurityTokenHandler().WriteToken(token); // Write the token and return it as a string
		}

		public async Task<RefreshTokenResponseDto> VerifyToken(TokenRequestDto tokenRequest) // Method to verify a token
		{
			string jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");

			JwtSecurityTokenHandler tokenHandler = new(); // Instantiate a token handler
			TokenValidationParameters tokenValidationParameters = new()
			{
				ValidateIssuerSigningKey = true, // Validate the signing key of the token
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), // Set the signing key from configuration
				ValidateIssuer = true,
				ValidIssuer = _configuration["Jwt:Issuer"],
				ValidateAudience = true,
				ValidAudience = _configuration["Jwt:Audience"],
				ClockSkew = TimeSpan.Zero // Avoid clock skew issues
			};

			try
			{
				//var principal = tokenHandler.ValidateToken(tokenRequest.Token, tokenValidationParameters, out var validatedToken); // Validate the token
				(ClaimsPrincipal principal, SecurityToken validatedToken) = await Task.Run(() =>
				{
					// Validate the token and capture principal and validatedToken
					ClaimsPrincipal principal = tokenHandler.ValidateToken(tokenRequest.Token, tokenValidationParameters, out SecurityToken? validatedToken);
					return (principal, validatedToken);
				});

				// Check if the token is expired
				if (validatedToken.ValidTo < DateTime.UtcNow) // Check if the token expiration time has passed
				{
					return new RefreshTokenResponseDto
					{
						IsValid = false, // Set token as invalid
						ErrorMessage = "Token is expired." // Return token expiration error message
					};
				}

				return new RefreshTokenResponseDto
				{
					IsValid = true, // Set token as valid
					Claims = [.. principal.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value })] // Map claims to a list of ClaimDto
				};
			}
			catch (Exception ex) // Catch any exceptions during token validation
			{
				return new RefreshTokenResponseDto
				{
					IsValid = false, // Set token as invalid in case of an error
					ErrorMessage = ex.Message // Return the error message
				};
			}
		}

		public JsonDocument DecodeJwtPayloadToJson(string token)
		{
			// Check if token is empty or null
			if (string.IsNullOrEmpty(token))
			{
				throw new ArgumentException("JWT token cannot be null or empty.");
			}

			// Split the token into parts
			string[] parts = token.Split('.');
			if (parts.Length < 3)
			{
				throw new ArgumentException("Invalid JWT token format.");
			}

			// Decode the payload (second part) from Base64
			string payload = parts[1];
			string base64Payload = payload.Replace('-', '+').Replace('_', '/'); // Standard Base64 format
			int padding = 4 - (base64Payload.Length % 4);
			if (padding < 4)
			{
				base64Payload += new string('=', padding);
			}

			byte[] bytes = Convert.FromBase64String(base64Payload);

			// Parse and return the decoded JSON
			return JsonDocument.Parse(bytes);
		}
	}
}