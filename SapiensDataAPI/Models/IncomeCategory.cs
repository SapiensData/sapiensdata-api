namespace SapiensDataAPI.Models;

public partial class IncomeCategory
{
	public int IncomeCategoryId { get; set; }

	public string CategoryName { get; set; } = null!;

	public string? Description { get; set; }

	public int? ParentCategoryId { get; set; }

	public virtual ICollection<Income> Incomes { get; set; } = [];

	public virtual ICollection<IncomeCategory> InverseParentCategory { get; set; } = [];

	public virtual IncomeCategory? ParentCategory { get; set; }
}
