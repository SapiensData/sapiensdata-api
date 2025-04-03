using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SapiensDataAPI.Dtos.Auth.Request;
using SapiensDataAPI.Models;
using SapiensDataAPI.Services.JwtToken;

namespace SapiensDataAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService) : ControllerBase
	{
		private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
		private readonly UserManager<ApplicationUser> _userManager = userManager;

		[HttpPost("register-user")]
		public async Task<IActionResult> Register([FromBody] RegisterRequestDto model)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest("Invalid registration details.");
			}

			if (model.Username.Contains("..") || model.Username.Contains('/') || model.Username.Contains('\\'))
			{
				return BadRequest("Invalid username. Username cannot contain '..' or '/' or '\\'.");
			}

			ApplicationUser? userExists = await _userManager.FindByNameAsync(model.Username);
			if (userExists != null)
			{
				return Conflict("Username already exists.");
			}

			ApplicationUser? emailExists = await _userManager.FindByEmailAsync(model.Email);
			if (emailExists != null)
			{
				return Conflict("Email is already in use.");
			}

			ApplicationUser user = new() { UserName = model.Username, Email = model.Email, FirstName = model.FirstName, LastName = model.LastName };

			Env.Load(".env");
			string drivePath = Environment.GetEnvironmentVariable("DRIVE_BEGINNING_PATH") ??
			                   throw new KeyNotFoundException("Drive path is not configured.");

			string userFolderPath = Path.Combine(drivePath, "SapiensCloud", "media", "user_data", user.UserName);

			if (!Directory.Exists(userFolderPath))
			{
				try
				{
					Directory.CreateDirectory(userFolderPath);
				}
				catch
				{
					throw new IOException("Failed to create user folder.");
				}
			}

			IdentityResult result = await _userManager.CreateAsync(user, model.Password);

			if (!result.Succeeded)
			{
				return BadRequest(result.Errors);
			}

			IdentityResult roleResult = await _userManager.AddToRoleAsync(user, "NormalUser");
			if (!roleResult.Succeeded)
			{
				// There is no reason to keep the user if the role assignment fails (I think)
				await _userManager.DeleteAsync(user);
				return BadRequest("Failed to assign role.");
			}

			return Ok("User registered and assigned role successfully.");
		}

		[HttpPost("user-login")]
		public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest("Invalid login details. Please provide a valid username and password.");
			}

			ApplicationUser? user = await _userManager.FindByNameAsync(model.Username);
			if (user == null)
			{
				return Unauthorized("Username does not exist.");
			}

			if (!await _userManager.CheckPasswordAsync(user, model.Password))
			{
				return Unauthorized("Incorrect password.");
			}

			string token = await _jwtTokenService.GenerateToken(user);

			if (string.IsNullOrEmpty(token))
			{
				return StatusCode(500, "An error occurred while generating the token.");
			}

			return Ok(token);
		}
	}
}