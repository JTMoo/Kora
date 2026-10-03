namespace StockManagement.Sales.Core;


/// <summary>
/// One VAT-rate bucket: lines grouped by rate, gross total split into taxed base and VAT share (#171).
/// </summary>
internal readonly record struct VatRateGroup(decimal Rate, decimal TaxedBase, decimal Vat)
{
	public decimal GrossTotal => this.TaxedBase + this.Vat;

	/// <remarks>Prices include VAT: for a rate of 10, the VAT share of the group's gross total is 10/110.</remarks>
	public static IEnumerable<VatRateGroup> ByRate(IEnumerable<(decimal Quantity, decimal UnitPrice, decimal VatRatePercent)> lines, int currencyDecimalDigits)
	{
		foreach (var group in lines.GroupBy(line => line.VatRatePercent))
		{
			var groupTotal = group.Sum(line => line.Quantity * Round(line.UnitPrice, currencyDecimalDigits));
			var vat = Round(groupTotal * group.Key / (100 + group.Key), currencyDecimalDigits);
			yield return new VatRateGroup(group.Key, groupTotal - vat, vat);
		}
	}

	private static decimal Round(decimal value, int digits)
	{
		return Math.Round(value, digits, MidpointRounding.AwayFromZero);
	}
}
