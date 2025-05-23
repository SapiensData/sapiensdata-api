﻿namespace SapiensDataAPI.Models
{
	public class Income
	{
		public required int IncomeId { get; set; }

		public required string UserId { get; set; }

		public DateOnly? IncomeDate { get; set; }

		public int? IncomeCategoryId { get; set; }

		public string? SourceType { get; set; }

		public string? SourceUserId { get; set; }

		public int? SourceCompanyId { get; set; }

		public string? SourceName { get; set; }

		public string? Description { get; set; }

		public decimal? GrossAmount { get; set; }

		public decimal? TaxAmount { get; set; }

		public decimal? NetAmount { get; set; }

		public string? Currency { get; set; }

		public bool? IsRecurring { get; set; }

		public int? FrequencyId { get; set; }

		public int? PaymentMethodId { get; set; }

		public string? PayerName { get; set; }

		public DateTime? ReceivedAt { get; set; }

		public DateTime? LastUpdatedAt { get; set; }

		public virtual Frequency Frequency { get; set; } = null!;

		public virtual IncomeCategory? IncomeCategory { get; set; }

		public virtual PaymentMethod? PaymentMethod { get; set; }

		public virtual Company? SourceCompany { get; set; }

		public virtual ApplicationUser? SourceUser { get; set; }

		public virtual ApplicationUser User { get; set; } = null!;
	}
}