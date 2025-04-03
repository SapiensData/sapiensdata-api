namespace SapiensDataAPI.Models
{
	public class TaxRate
	{
		public int TaxRateId { get; set; }

		public string? TaxCode { get; set; }

		public decimal? VatRate { get; set; }

		public string? Description { get; set; }

		public decimal? NetAmount { get; set; }

		public decimal? VatAmount { get; set; }

		public int? ReceiptId { get; set; }

		public virtual ICollection<Expense> Expenses { get; set; } = [];

		public virtual Receipt? Receipt { get; set; }

		public virtual ICollection<ReceiptTaxDetail> ReceiptTaxDetails { get; set; } = [];
	}
}