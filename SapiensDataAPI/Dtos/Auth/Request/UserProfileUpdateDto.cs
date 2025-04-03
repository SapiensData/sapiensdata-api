namespace SapiensDataAPI.Dtos.Auth.Request
{
	public class UserProfileUpdateDto
	{
		public string? Username { get; set; }
		public string? Email { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
	}
}