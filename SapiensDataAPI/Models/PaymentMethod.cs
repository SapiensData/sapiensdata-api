namespace SapiensDataAPI.Models;

public partial class PaymentMethod
{
	public int PaymentMethodId { get; set; }

	public string? Name { get; set; }

	public string? Abbreviation { get; set; }

	public string? Description { get; set; }

	public virtual ICollection<Expense> Expenses { get; set; } = [];

	public virtual ICollection<Income> Incomes { get; set; } = [];

	public virtual ICollection<ReceiptPayment> ReceiptPayments { get; set; } = [];

	public virtual ICollection<Receipt> Receipts { get; set; } = [];
}
