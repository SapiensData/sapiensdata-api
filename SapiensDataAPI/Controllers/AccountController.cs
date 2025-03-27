using AutoMapper;
using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SapiensDataAPI.Data.DbContextCs;
using SapiensDataAPI.Dtos.Auth.Request;
using SapiensDataAPI.Dtos.ImageUploader.Request;
using SapiensDataAPI.Models;
using SapiensDataAPI.Services.JwtToken;
using System.Globalization;
using System.Security.Claims;

namespace SapiensDataAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountController(
		UserManager<ApplicationUser> userManager,
		RoleManager<IdentityRole> roleManager,
		IJwtTokenService jwtTokenService,
		SapiensDataDbContext context,
		IMapper mapper) : ControllerBase
	{
		private readonly SapiensDataDbContext _context = context;
		private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
		private readonly IMapper _mapper = mapper;
		private readonly RoleManager<IdentityRole> _roleManager = roleManager;
		private readonly UserManager<ApplicationUser> _userManager = userManager;

		[HttpGet("get-all-users")]
		public async Task<IActionResult> GetUsers()
		{
			List<ApplicationUser> users = await _userManager.Users.ToListAsync();
			List<object> usersWithRoles = [];

			foreach (ApplicationUser? user in users)
			{
				IList<string> roles = await _userManager.GetRolesAsync(user);

				// Create an anonymous object containing the user's details and roles
				var userWithRoles = new
				{
					user.FirstName,
					user.LastName,
					user.Id,
					user.UserName,
					user.NormalizedUserName,
					user.Email,
					user.NormalizedEmail,
					user.EmailConfirmed,
					user.PasswordHash,
					user.SecurityStamp,
					user.ConcurrencyStamp,
					user.PhoneNumber,
					user.PhoneNumberConfirmed,
					user.TwoFactorEnabled,
					user.LockoutEnd,
					user.LockoutEnabled,
					user.AccessFailedCount,
					Roles = roles.ToList()
				};

				usersWithRoles.Add(userWithRoles);
			}

			return Ok(usersWithRoles);
		}

		[HttpGet("get-user-by-username/{username}")]
		public async Task<IActionResult> GetUserByUsername(string username)
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				return BadRequest("Username cannot be empty.");
			}

			ApplicationUser? user = await _userManager.FindByNameAsync(username);
			if (user == null)
			{
				return NotFound($"User with username '{username}' was not found.");
			}

			IList<string> roles = await _userManager.GetRolesAsync(user);

			// Create an anonymous object containing the user's details and roles
			var userWithRoles = new
			{
				user.FirstName,
				user.LastName,
				user.Id,
				user.UserName,
				user.NormalizedUserName,
				user.Email,
				user.NormalizedEmail,
				user.EmailConfirmed,
				user.PhoneNumber,
				user.PhoneNumberConfirmed,
				user.TwoFactorEnabled,
				user.LockoutEnd,
				user.LockoutEnabled,
				user.AccessFailedCount,
				Roles = roles.ToList()
			};

			return Ok(userWithRoles);
		}

		[HttpPost("upload-pfp")]
		[Authorize]
		public async Task<IActionResult> UploadPfp([FromForm] UploadImageDto image)
		{
			string? username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(username))
			{
				return Unauthorized("User couldn't be identified.");
			}

			if (image.Image.Length == 0)
			{
				return BadRequest("No image file provided.");
			}

			Env.Load(".env");
			string drivePath = Environment.GetEnvironmentVariable("DRIVE_BEGINNING_PATH") ??
			                   throw new KeyNotFoundException("Drive path is not configured.");

			string uploadsFolderPath = Path.Combine(drivePath, "SapiensCloud", "media", "user_data", username);

			if (!Directory.Exists(uploadsFolderPath))
			{
				try
				{
					Directory.CreateDirectory(uploadsFolderPath);
				}
				catch
				{
					throw new IOException("Failed to create user folder.");
				}
			}

			string extension = Path.GetExtension(image.Image.FileName);
			string newFileName = "profile-picture_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + extension;

			string filePath = Path.Combine(uploadsFolderPath, newFileName);

			await using (FileStream fileStream = new(filePath, FileMode.Create))
			{
				await image.Image.CopyToAsync(fileStream);
			}

			ApplicationUser? user = await _userManager.FindByNameAsync(username);
			if (user == null)
			{
				return NotFound("User was not found.");
			}

			user.ProfilePicturePath = filePath;
			_context.Update(user);
			await _context.SaveChangesAsync();

			return Ok("Image uploaded successfully.");
		}

		[HttpDelete("admin/delete-user/{username}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AdminDeleteUser(string username)
		{
			ApplicationUser? user = await _userManager.FindByNameAsync(username);
			if (user == null)
			{
				return NotFound($"User with username '{username}' not found.");
			}

			IdentityResult result = await _userManager.DeleteAsync(user);
			if (!result.Succeeded)
			{
				return BadRequest(result.Errors.Select(e => e.Description));
			}

			return Ok("User deleted successfully.");
		}

		[HttpDelete("delete-my-account")]
		[Authorize]
		public async Task<IActionResult> DeleteMyAccount()
		{
			string? username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(username))
			{
				return Unauthorized("User couldn't be identified.");
			}

			ApplicationUser? user = await _userManager.FindByNameAsync(username);
			if (user == null)
			{
				return NotFound("User not found.");
			}

			IdentityResult result = await _userManager.DeleteAsync(user);
			if (!result.Succeeded)
			{
				return BadRequest(result.Errors.Select(e => e.Description));
			}

			return Ok("Your account has been deleted successfully.");
		}

		[HttpPut("admin/update-user/{username}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AdminUpdateUser(string username, [FromBody] AdminUpdateUserDto model)
		{
			ApplicationUser? user = await _userManager.FindByNameAsync(username);
			if (user == null)
			{
				return NotFound($"User with username '{username}' not found.");
			}

			user.UserName = model.Username;
			user.Email = model.Email;
			user.FirstName = model.FirstName;
			user.LastName = model.LastName;

			IdentityResult updateResult = await _userManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				return BadRequest($"Error updating user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
			}

			// ChangePasswordAsync doesn't work here because we don't have the old password
			if (!string.IsNullOrEmpty(model.Password))
			{
				IdentityResult passwordRemovalResult = await _userManager.RemovePasswordAsync(user);
				if (passwordRemovalResult.Succeeded)
				{
					IdentityResult addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
					if (!addPasswordResult.Succeeded)
					{
						return BadRequest(
							$"Error setting password: {string.Join(", ", addPasswordResult.Errors.Select(e => e.Description))}");
					}
				}
				else
				{
					return BadRequest(
						$"Error removing password: {string.Join(", ", passwordRemovalResult.Errors.Select(e => e.Description))}");
				}
			}

			return Ok($"User '{username}' updated successfully by admin.");
		}

		[HttpPut("update-my-profile")]
		[Authorize]
		public async Task<IActionResult> UpdateMyProfile([FromBody] UserProfileUpdateDto model)
		{
			string? username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(username))
			{
				return Unauthorized("User couldn't be identified.");
			}

			ApplicationUser? user = await _userManager.FindByNameAsync(username);
			if (user == null)
			{
				return NotFound("User not found.");
			}

			user.Email = model.Email ?? user.Email;
			user.FirstName = model.FirstName ?? user.FirstName;
			user.LastName = model.LastName ?? user.LastName;

			IdentityResult updateResult = await _userManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				return BadRequest(
					$"Error updating your profile: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
			}

			return Ok("Your profile has been updated successfully.");
		}

		[HttpPut("admin/add-role")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AddUserRole([FromBody] ChangeUserRoleRequestDto model)
		{
			if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.RoleName))
			{
				return BadRequest("Invalid input.");
			}

			ApplicationUser? user = await _userManager.FindByNameAsync(model.Username);
			if (user == null)
			{
				return NotFound("User not found.");
			}

			bool roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
			if (!roleExists)
			{
				return BadRequest("Role does not exist.");
			}

			bool userRoleExists = await _userManager.IsInRoleAsync(user, model.RoleName);
			if (userRoleExists)
			{
				return BadRequest("User already has the role.");
			}

			IdentityResult result = await _userManager.AddToRoleAsync(user, model.RoleName);

			if (!result.Succeeded)
			{
				return BadRequest("Failed to add role to user.");
			}

			return Ok($"Role {model.RoleName} added to user successfully.");
		}

		[HttpPut("admin/remove-role")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> RemoveUserRole([FromBody] ChangeUserRoleRequestDto model)
		{
			if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.RoleName))
			{
				return BadRequest("Invalid input.");
			}

			ApplicationUser? user = await _userManager.FindByNameAsync(model.Username);
			if (user == null)
			{
				return NotFound("User not found.");
			}

			bool roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
			if (!roleExists)
			{
				return BadRequest("Role does not exist.");
			}

			bool userRoleExists = await _userManager.IsInRoleAsync(user, model.RoleName);
			if (!userRoleExists)
			{
				return BadRequest("User does not have the role.");
			}

			IdentityResult result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);

			if (!result.Succeeded)
			{
				return BadRequest("Failed to remove user role.");
			}

			return Ok($"User role removed to {model.RoleName} successfully.");
		}
	}
}