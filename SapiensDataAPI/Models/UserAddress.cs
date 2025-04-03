namespace SapiensDataAPI.Models
{
	public class UserAddress
	{
		public int CompanyAddressId { get; set; }

		public string? UserId { get; set; }

		public int? AddressId { get; set; }

		public bool? IsDefault { get; set; }

		public string? AddressType { get; set; }

		public DateTime? CreatedAt { get; set; }

		public DateTime? UpdatedAt { get; set; }

		public virtual Address? Address { get; set; }

		public virtual ApplicationUser? User { get; set; }
	}
}