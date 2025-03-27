namespace SapiensDataAPI.Models
{
	public class Company
	{
		public int CompanyId { get; set; }

		public string Name { get; set; } = null!;

		public string? Industry { get; set; }

		public string? Description { get; set; }

		public string? RegistrationNumber { get; set; }

		public string? TaxId { get; set; }

		public string? Website { get; set; }

		public string? ContactEmail { get; set; }

		public string? ContactPhone { get; set; }

		public DateOnly? FoundedDate { get; set; }

		public virtual ICollection<CompanyAddress> CompanyAddresses { get; set; } = [];

		public virtual ICollection<Debt> Debts { get; set; } = [];

		public virtual ICollection<Expense> Expenses { get; set; } = [];

		public virtual ICollection<Income> Incomes { get; set; } = [];

		public virtual ICollection<Investment> Investments { get; set; } = [];
	}
}