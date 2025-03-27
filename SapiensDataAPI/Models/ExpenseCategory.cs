namespace SapiensDataAPI.Models
{
	public class ExpenseCategory
	{
		public int ExpenseCategoryId { get; set; }

		public string CategoryName { get; set; } = null!;

		public string? Description { get; set; }

		public decimal? Budget { get; set; }

		public int? ParentCategoryId { get; set; }

		public virtual ICollection<Expense> Expenses { get; set; } = [];

		public virtual ICollection<ExpenseCategory> InverseParentCategory { get; set; } = [];

		public virtual ExpenseCategory? ParentCategory { get; set; }
	}
}