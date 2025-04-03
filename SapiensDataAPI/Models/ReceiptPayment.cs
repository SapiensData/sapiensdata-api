namespace SapiensDataAPI.Models
{
	public class ReceiptPayment
	{
		public int ReceiptPaymentId { get; set; }

		public int? ReceiptId { get; set; }

		public int? PaymentMethodId { get; set; }

		public decimal? Amount { get; set; }

		public virtual PaymentMethod? PaymentMethod { get; set; }

		public virtual Receipt? Receipt { get; set; }
	}
}