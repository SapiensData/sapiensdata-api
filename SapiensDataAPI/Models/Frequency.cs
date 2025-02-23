namespace SapiensDataAPI.Models;

public partial class Frequency
{
	public int FrequencyId { get; set; }

	public string FrequencyName { get; set; } = null!;

	public string? Description { get; set; }

	public int? DaysInterval { get; set; }

	public virtual ICollection<Expense> Expenses { get; set; } = [];

	public virtual ICollection<Income> Incomes { get; set; } = [];

	public virtual ICollection<Saving> Savings { get; set; } = [];
}
