using AutoMapper;
using SapiensDataAPI.Dtos.Expense.Request;
using SapiensDataAPI.Dtos.Income.Request;
using SapiensDataAPI.Dtos.Receipt.JSON;
using SapiensDataAPI.Dtos.Receipt.Request;
using SapiensDataAPI.Models;
using System.Globalization;

namespace SapiensDataAPI.Configs
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<IncomeDto, Income>();
			CreateMap<ExpenseDto, Expense>();
			CreateMap<ReceiptDto, Receipt>();
			CreateMap<ReceiptV, Receipt>();
			CreateMap<TaxRateV, TaxRate>();
			CreateMap<ReceiptTaxDetailV, ReceiptTaxDetail>();

			CreateMap<StoreV, Store>();
			CreateMap<StoreV, Address>();

			CreateMap<Store, StoreV>();
			CreateMap<Address, StoreV>();

			CreateMap<Product, ProductV>();
			CreateMap<TaxRate, TaxRateV>();
			CreateMap<ReceiptTaxDetail, ReceiptTaxDetailV>();
			CreateMap<Store, StoreV>();
			CreateMap<Receipt, ReceiptV>();

			// Define mapping for string to nullable types without using out arguments
			CreateMap<string, int?>().ConvertUsing(s => ParseToNullableInt(s));
			CreateMap<string, decimal?>().ConvertUsing(s => ParseToNullableDecimal(s));
			CreateMap<string, bool?>().ConvertUsing(s => ParseToNullableBool(s));
			CreateMap<string, DateTime?>().ConvertUsing(s => ParseToNullableDateTime(s));

			// If you have more complex mappings
			CreateMap<ProductV, Product>()
				.ForMember(dest => dest.ExpirationDate,
					opt => opt.MapFrom(src => ParseToNullableDateTime(src.ExpirationDate)));
		}

		// Helper method to parse string to nullable int
		private static int? ParseToNullableInt(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return null;
			}

			if (int.TryParse(s, out int result))
			{
				return result;
			}

			return null;
		}

		// Helper method to parse string to nullable decimal
		private static decimal? ParseToNullableDecimal(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return null;
			}

			string normalizedSource = s.Replace('.', ',');

			if (decimal.TryParse(normalizedSource, out decimal result))
			{
				return result;
			}

			return null;
		}

		// Helper method to parse string to nullable bool
		private static bool? ParseToNullableBool(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return null;
			}

			switch (s)
			{
				case "0":
					return false;
				case "1":
					return true;
			}

			if (bool.TryParse(s, out bool result))
			{
				return result;
			}

			return null;
		}

		// Helper method to parse string to nullable DateTime
		private static DateTime? ParseToNullableDateTime(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return null;
			}

			if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
			{
				return result;
			}

			return null;
		}
	}
}