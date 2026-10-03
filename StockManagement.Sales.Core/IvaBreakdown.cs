using StockManagement.Kernel.Model;

namespace StockManagement.Sales.Core;


/// <summary>
/// Splits invoice lines into Paraguay's three IVA buckets (10%/5%/exempt), same grouping as <see cref="InvoiceCalculator.CalculateTax"/>
/// but kept per rate instead of summed, for the Marangatu export (#123).
/// </summary>
internal readonly record struct IvaBreakdown(decimal Taxed10, decimal Vat10, decimal Taxed5, decimal Vat5, decimal Exempt)
{
	public static IvaBreakdown Calculate(IEnumerable<ShoppingCartItem> items, int currencyDecimalDigits)
	{
		decimal taxed10 = 0, vat10 = 0, taxed5 = 0, vat5 = 0, exempt = 0;
		foreach (var group in VatRateGroup.ByRate((items ?? []).Select(item => ((decimal)item.Amount, item.StockItem.Price, item.StockItem.VatRatePercent)), currencyDecimalDigits))
		{
			if (group.Rate == 10) { taxed10 += group.TaxedBase; vat10 += group.Vat; }
			else if (group.Rate == 5) { taxed5 += group.TaxedBase; vat5 += group.Vat; }
			else exempt += group.GrossTotal;
		}

		return new IvaBreakdown(taxed10, vat10, taxed5, vat5, exempt);
	}
}
