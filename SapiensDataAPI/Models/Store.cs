namespace SapiensDataAPI.Models
{
	public class Store
	{
		public int StoreId { get; set; }

		public string? Name { get; set; }

		public string? BrandName { get; set; }

		public string? TaxId { get; set; }

		public virtual ICollection<Receipt> Receipts { get; set; } = [];

		public virtual ICollection<StoreAddress> StoreAddresses { get; set; } = [];
	}
}