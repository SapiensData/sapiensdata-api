namespace SapiensDataAPI.Models
{
	public class UserSession
	{
		public int SessionId { get; set; }

		public required string UserId { get; set; }

		public DateTime? LoginTime { get; set; }

		public DateTime? LogoutTime { get; set; }

		public string? IpAddress { get; set; }

		public string? Device { get; set; }

		public string? Browser { get; set; }

		public string? OperatingSystem { get; set; }

		public string? SessionToken { get; set; }

		public bool? IsActive { get; set; }

		public string? Location { get; set; }

		public int? LoginAttempts { get; set; }

		public int? FailedLoginAttempts { get; set; }

		public int? SessionDuration { get; set; }

		public virtual ApplicationUser User { get; set; } = null!;
	}
}