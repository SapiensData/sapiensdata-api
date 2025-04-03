using SapiensDataAPI.Dtos.Auth.Request;
using SapiensDataAPI.Dtos.Auth.Response;
using SapiensDataAPI.Models;

namespace SapiensDataAPI.Services.JwtToken
{
	public interface IJwtTokenService
	{
		Task<string> GenerateToken(ApplicationUser user);
		Task<RefreshTokenResponseDto> VerifyToken(TokenRequestDto tokenRequest);
	}
}