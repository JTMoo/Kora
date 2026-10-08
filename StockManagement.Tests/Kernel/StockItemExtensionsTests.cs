using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.ExtensionMethods;

namespace StockManagement.Tests.Kernel;


[TestClass]
public sealed class StockItemExtensionsTests
{
	[TestMethod]
	public void GetManufacturers_NullItems_ReturnsEmpty()
	{
		// Act
		var result = ((IEnumerable<StockItem>)null).GetManufacturers();

		// Assert
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public void GetManufacturers_MixedCaseAndEmpty_ReturnsDistinctSorted()
	{
		// Arrange
		List<StockItem> stockItems =
		[
			new("a", manufacturer: "Samasz"),
			new("b", manufacturer: ""),
			new("c", manufacturer: "hattat"),
			new("d", manufacturer: "SAMASZ"),
			new("e", manufacturer: "Hattat")
		];

		// Act
		var result = stockItems.GetManufacturers();

		// Assert
		CollectionAssert.AreEqual(new[] { "hattat", "Samasz" }, result.ToList());
	}

	[TestMethod]
	public void Manufacturer_SurroundingWhitespace_IsTrimmed()
	{
		// Act
		var stockItem = new StockItem("a", manufacturer: "  Kuhn ");

		// Assert
		Assert.AreEqual("Kuhn", stockItem.Manufacturer);
	}

	[TestMethod]
	public void CalculateSalePrice_AppliesFactorToCostPlusAdditionalCost_RoundsToCurrencyDigits()
	{
		// Act
		var result = StockItemExtensions.CalculateSalePrice(purchasePrice: 10m, purchaseExchangeRate: 7300m, additionalPurchaseCost: 5000m, factor: 1.25m, currencyDecimalDigits: 0);

		// Assert
		Assert.AreEqual(97500m, result);
	}

	[TestMethod]
	public void CalculateSalePrice_ZeroFactor_ReturnsZero()
	{
		// Act
		var result = StockItemExtensions.CalculateSalePrice(purchasePrice: 10m, purchaseExchangeRate: 7300m, additionalPurchaseCost: 5000m, factor: 0m, currencyDecimalDigits: 0);

		// Assert
		Assert.AreEqual(0m, result);
	}

	[TestMethod]
	public void CalculateSalePrice_MidpointRoundsAwayFromZero()
	{
		// Act
		var result = StockItemExtensions.CalculateSalePrice(purchasePrice: 1m, purchaseExchangeRate: 1m, additionalPurchaseCost: 0.005m, factor: 1m, currencyDecimalDigits: 2);

		// Assert
		Assert.AreEqual(1.01m, result);
	}

	[TestMethod]
	public void TryAllocateLandedCost_SplitsProportionalToPurchaseValue()
	{
		// Arrange
		var cheap = new StockItem("cheap") { Id = "a", PurchasePrice = 10m, PurchaseExchangeRate = 1m, Amount = 10 }; // value 100
		var expensive = new StockItem("expensive") { Id = "b", PurchasePrice = 10m, PurchaseExchangeRate = 1m, Amount = 30 }; // value 300

		// Act
		var ok = StockItemExtensions.TryAllocateLandedCost([cheap, expensive], totalLandedCost: 400m, currencyDecimalDigits: 0, out var allocations);

		// Assert
		Assert.IsTrue(ok);
		Assert.AreEqual(100m, allocations.Single(a => a.StockItemId == "a").AllocatedCost);
		Assert.AreEqual(10m, allocations.Single(a => a.StockItemId == "a").AdditionalPurchaseCostPerUnit); // 100 / 10 units
		Assert.AreEqual(300m, allocations.Single(a => a.StockItemId == "b").AllocatedCost);
		Assert.AreEqual(10m, allocations.Single(a => a.StockItemId == "b").AdditionalPurchaseCostPerUnit); // 300 / 30 units
	}

	[TestMethod]
	public void TryAllocateLandedCost_ZeroAmountItem_GetsZeroPerUnitCost()
	{
		// Arrange
		var item = new StockItem("a") { Id = "a", PurchasePrice = 10m, PurchaseExchangeRate = 1m, Amount = 0 };
		var other = new StockItem("b") { Id = "b", PurchasePrice = 10m, PurchaseExchangeRate = 1m, Amount = 10 };

		// Act
		var ok = StockItemExtensions.TryAllocateLandedCost([item, other], totalLandedCost: 100m, currencyDecimalDigits: 0, out var allocations);

		// Assert
		Assert.IsTrue(ok);
		Assert.AreEqual(0m, allocations.Single(a => a.StockItemId == "a").AdditionalPurchaseCostPerUnit);
	}

	[TestMethod]
	public void TryAllocateLandedCost_NoPurchaseValue_ReturnsFalse()
	{
		// Arrange
		var items = new List<StockItem> { new("a") { Id = "a", PurchasePrice = 0m, Amount = 10 } };

		// Act
		var ok = StockItemExtensions.TryAllocateLandedCost(items, totalLandedCost: 100m, currencyDecimalDigits: 0, out var allocations);

		// Assert
		Assert.IsFalse(ok);
		Assert.AreEqual(0, allocations.Count);
	}

	[TestMethod]
	public void CalculatePurchaseTotal_SumsAmountTimesUnitPrice_RoundsToCurrencyDigits()
	{
		// Arrange
		var itemA = new SupplierInvoiceItem(new StockItem("a")) { Amount = 3, UnitPrice = 10.004m };
		var itemB = new SupplierInvoiceItem(new StockItem("b")) { Amount = 2, UnitPrice = 5.006m };

		// Act
		var result = StockItemExtensions.CalculatePurchaseTotal([itemA, itemB], currencyDecimalDigits: 2);

		// Assert: 3*10.004 + 2*5.006 = 30.012 + 10.012 = 40.024 -> rounds to 40.02
		Assert.AreEqual(40.02m, result);
	}

	[TestMethod]
	public void CalculatePurchaseTotal_NullItems_ReturnsZero()
	{
		// Act
		var result = StockItemExtensions.CalculatePurchaseTotal(null, currencyDecimalDigits: 0);

		// Assert
		Assert.AreEqual(0m, result);
	}
}
