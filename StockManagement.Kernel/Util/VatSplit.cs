namespace StockManagement.Kernel.Util;


public static class VatSplit
{
	/// <remarks>Prices include VAT: for a rate of 10, the VAT share of a gross amount is amount*10/110.</remarks>
	public static decimal VatShare(decimal grossAmount, decimal vatRatePercent, int currencyDecimalDigits)
	{
		return Math.Round(grossAmount * vatRatePercent / (100 + vatRatePercent), currencyDecimalDigits, MidpointRounding.AwayFromZero);
	}
}
