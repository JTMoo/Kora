namespace StockManagement.Kernel.Model.ExtensionMethods;


public static class StockItemExtensions
{
	/// <summary>
	/// Sale price derived from purchase data: <c>(purchasePrice * exchangeRate + additionalPurchaseCost) * factor</c>.
	/// </summary>
	/// <remarks>Rounds to <paramref name="currencyDecimalDigits"/> using <see cref="MidpointRounding.AwayFromZero"/>, same as <c>InvoiceCalculator</c> (ADR-0014).</remarks>
	public static decimal CalculateSalePrice(decimal purchasePrice, decimal purchaseExchangeRate, decimal additionalPurchaseCost, decimal factor, int currencyDecimalDigits)
	{
		var cost = (purchasePrice * purchaseExchangeRate) + additionalPurchaseCost;
		return Math.Round(cost * factor, currencyDecimalDigits, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// One stock item's share of a landed cost (freight + broker fee + duties), split proportional to purchase value.
	/// </summary>
	public readonly record struct LandedCostAllocation(string StockItemId, decimal AllocatedCost, decimal AdditionalPurchaseCostPerUnit);

	/// <summary>
	/// Splits <paramref name="totalLandedCost"/> across <paramref name="stockItems"/> proportional to each item's
	/// purchase value (<c>PurchasePrice * PurchaseExchangeRate * Amount</c>), overwriting no state itself.
	/// </summary>
	/// <remarks>Rounds to <paramref name="currencyDecimalDigits"/> using <see cref="MidpointRounding.AwayFromZero"/>, same as <see cref="CalculateSalePrice"/>.</remarks>
	/// <returns>False when every item's purchase value is 0 - nothing to allocate proportionally against</returns>
	public static bool TryAllocateLandedCost(IReadOnlyList<StockItem> stockItems, decimal totalLandedCost, int currencyDecimalDigits, out IReadOnlyList<LandedCostAllocation> allocations)
	{
		var totalValue = stockItems.Sum(item => item.PurchasePrice * item.PurchaseExchangeRate * item.Amount);
		if (totalValue <= 0)
		{
			allocations = [];
			return false;
		}

		var result = new List<LandedCostAllocation>(stockItems.Count);
		foreach (var item in stockItems)
		{
			var itemValue = item.PurchasePrice * item.PurchaseExchangeRate * item.Amount;
			var allocatedCost = Math.Round(totalLandedCost * itemValue / totalValue, currencyDecimalDigits, MidpointRounding.AwayFromZero);
			var perUnit = item.Amount > 0 ? Math.Round(allocatedCost / item.Amount, currencyDecimalDigits, MidpointRounding.AwayFromZero) : 0m;
			result.Add(new LandedCostAllocation(item.Id, allocatedCost, perUnit));
		}

		allocations = result;
		return true;
	}

	/// <summary>
	/// Sum of <c>Amount*UnitPrice</c> across a domestic purchase's lines (#246), rounded once at the end.
	/// </summary>
	/// <remarks>Rounds to <paramref name="currencyDecimalDigits"/> using <see cref="MidpointRounding.AwayFromZero"/>, same as <see cref="CalculateSalePrice"/>.</remarks>
	public static decimal CalculatePurchaseTotal(IEnumerable<SupplierInvoiceItem> items, int currencyDecimalDigits)
	{
		var total = (items ?? []).Sum(item => item.Amount * item.UnitPrice);
		return Math.Round(total, currencyDecimalDigits, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// Distinct manufacturer names in use, sorted.
	/// </summary>
	/// <remarks>Empty names are skipped; names differing only in case count once (first spelling wins).</remarks>
	public static IReadOnlyList<string> GetManufacturers(this IEnumerable<StockItem> stockItems)
	{
		if (stockItems == null) return [];

		return stockItems
			.Select(stockItem => stockItem.Manufacturer)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Order(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}
}
