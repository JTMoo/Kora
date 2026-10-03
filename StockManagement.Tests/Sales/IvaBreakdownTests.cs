using StockManagement.Kernel.Model;
using StockManagement.Sales.Core;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class IvaBreakdownTests
{
	[TestMethod]
	public void Calculate_NoItems_ReturnsZero()
	{
		// Act
		var result = IvaBreakdown.Calculate([], 0);

		// Assert
		Assert.AreEqual(new IvaBreakdown(0, 0, 0, 0, 0), result);
	}

	[TestMethod]
	public void Calculate_TenPercentLine_SplitsGrossIntoTaxedBaseAndVat()
	{
		// Arrange: 110 gross @10% -> 100 taxed base + 10 VAT
		List<ShoppingCartItem> items = [new ShoppingCartItem(new StockItem("Soda", price: 110, amount: 5) { VatRatePercent = 10 }) { Amount = 1 }];

		// Act
		var result = IvaBreakdown.Calculate(items, 0);

		// Assert
		Assert.AreEqual(new IvaBreakdown(100, 10, 0, 0, 0), result);
	}

	[TestMethod]
	public void Calculate_FivePercentLine_SplitsGrossIntoTaxedBaseAndVat()
	{
		// Arrange: 105 gross @5% -> 100 taxed base + 5 VAT
		List<ShoppingCartItem> items = [new ShoppingCartItem(new StockItem("Milk", price: 105, amount: 5) { VatRatePercent = 5 }) { Amount = 1 }];

		// Act
		var result = IvaBreakdown.Calculate(items, 0);

		// Assert
		Assert.AreEqual(new IvaBreakdown(0, 0, 100, 5, 0), result);
	}

	[TestMethod]
	public void Calculate_ZeroPercentLine_IsExemptGross()
	{
		// Arrange
		List<ShoppingCartItem> items = [new ShoppingCartItem(new StockItem("Bread", price: 50, amount: 5) { VatRatePercent = 0 }) { Amount = 2 }];

		// Act
		var result = IvaBreakdown.Calculate(items, 0);

		// Assert
		Assert.AreEqual(new IvaBreakdown(0, 0, 0, 0, 100), result);
	}

	[TestMethod]
	public void Calculate_MixedRates_GroupsEachRateSeparately()
	{
		// Arrange
		List<ShoppingCartItem> items =
		[
			new ShoppingCartItem(new StockItem("Soda", price: 110, amount: 5) { VatRatePercent = 10 }) { Amount = 1 },
			new ShoppingCartItem(new StockItem("Milk", price: 105, amount: 5) { VatRatePercent = 5 }) { Amount = 1 },
			new ShoppingCartItem(new StockItem("Bread", price: 50, amount: 5) { VatRatePercent = 0 }) { Amount = 1 }
		];

		// Act
		var result = IvaBreakdown.Calculate(items, 0);

		// Assert
		Assert.AreEqual(new IvaBreakdown(100, 10, 100, 5, 50), result);
	}

	[TestMethod]
	public void Calculate_MixedRates_Vat10Plus5MatchesInvoiceCalculatorCalculateTax()
	{
		// Arrange (#171): both read off the same VatRateGroup.ByRate, so they can't drift apart
		List<ShoppingCartItem> items =
		[
			new ShoppingCartItem(new StockItem("Soda", price: 110, amount: 5) { VatRatePercent = 10 }) { Amount = 1 },
			new ShoppingCartItem(new StockItem("Milk", price: 105, amount: 5) { VatRatePercent = 5 }) { Amount = 1 }
		];
		var lines = items.Select(item => new SaleLine(item.StockItem.Code, item.StockItem.Name, item.Amount, item.StockItem.Price, item.StockItem.VatRatePercent));

		// Act
		var breakdown = IvaBreakdown.Calculate(items, 0);
		var tax = InvoiceCalculator.CalculateTax(lines, 0);

		// Assert
		Assert.AreEqual(breakdown.Vat10 + breakdown.Vat5, tax);
	}
}
