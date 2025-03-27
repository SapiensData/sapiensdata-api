// SapiensDataAPI/Services/JwtTokenService.cs

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SapiensDataAPI.Dtos.Auth.Request;
using SapiensDataAPI.Dtos.Auth.Response;
using SapiensDataAPI.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SapiensDataAPI.Services.JwtToken
{
	public class JwtTokenService(UserManager<ApplicationUser> userManager, IConfiguration configuration) : IJwtTokenService
	{
		private readonly IConfiguration _configuration = configuration;
		private readonly UserManager<ApplicationUser> _userManager = userManager;

		public async Task<string> GenerateToken(ApplicationUser user)
		{
			IList<string> roles = await _userManager.GetRolesAsync(user);

			if (string.IsNullOrEmpty(user.UserName))
			{
				throw new InvalidOperationException("No username provided.");
			}

			IEnumerable<Claim> claims =
			[
				new(JwtRegisteredClaimNames.Sub, user.UserName),
				new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
				// Add each user role as a claim
				.. roles.Select(role => new Claim("role", role))
			];

			string? jwtKey = _configuration["Jwt:Key"];

			if (string.IsNullOrEmpty(jwtKey))
			{
				throw new InvalidOperationException("JWT Key is not configured in the settings.");
			}

			SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtKey));

			SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

			JwtSecurityToken token = new(
				_configuration["Jwt:Issuer"],
				_configuration["Jwt:Audience"],
				claims,
				expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:DurationInMinutes"], CultureInfo.InvariantCulture)),
				signingCredentials: credentials);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		// Why does this exist when it's never used, I think that the identity framework already verifies the tokens
		public async Task<RefreshTokenResponseDto> VerifyToken(TokenRequestDto tokenRequest)
		{
			string jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");

			JwtSecurityTokenHandler tokenHandler = new();
			TokenValidationParameters tokenValidationParameters = new()
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
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
					ClaimsPrincipal principal =
						tokenHandler.ValidateToken(tokenRequest.Token, tokenValidationParameters, out SecurityToken? validatedToken);
					return (principal, validatedToken);
				});

				// Check if the token is expired
				if (validatedToken.ValidTo < DateTime.UtcNow)
				{
					return new RefreshTokenResponseDto { IsValid = false, ErrorMessage = "Token is expired." };
				}

				return new RefreshTokenResponseDto
				{
					IsValid = true,
					// Map claims to a list of ClaimDto
					Claims = [.. principal.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value })]
				};
			}
			catch (Exception ex)
			{
				return new RefreshTokenResponseDto { IsValid = false, ErrorMessage = ex.Message };
			}
		}
	}
}