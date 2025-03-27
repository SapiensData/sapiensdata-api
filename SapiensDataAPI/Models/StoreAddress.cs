﻿namespace SapiensDataAPI.Models
{
	public class StoreAddress
	{
		public int CompanyAddressId { get; set; }

		public int? StoreId { get; set; }

		public int? AddressId { get; set; }

		public bool? IsDefault { get; set; }

		public string? AddressType { get; set; } = string.Empty;

		public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

		public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

		public virtual Address? Address { get; set; }

		public virtual Store? Store { get; set; }
	}
}