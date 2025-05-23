﻿using SapiensDataAPI.Dtos.Receipt.JSON;

namespace SapiensDataAPI.Dtos.Receipt.Response
{
	public class ResReceiptDto
	{
		public string FileName { get; set; } = string.Empty;
		public string ContentType { get; set; } = string.Empty;
		public DateTime? UploadDate { get; set; }
		public StoreV Store { get; set; } = new();
		public List<ProductV> Product { get; set; } = [];
		public ReceiptV Receipt { get; set; } = new();
		public List<TaxRateV> TaxRate { get; set; } = [];
		public List<ReceiptTaxDetailV> ReceiptTaxDetail { get; set; } = [];
		public string ImageData { get; set; } = string.Empty;
	}
}