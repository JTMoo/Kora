using StockManagement.Kernel.Util;

namespace StockManagement.Tests.Kernel;


[TestClass]
public sealed class VatSplitTests
{
	[TestMethod]
	public void VatShare_TenPercent_SplitsGrossCorrectly()
	{
		// Arrange (#210): 110 gross @10% -> 10 VAT
		// Act
		var result = VatSplit.VatShare(110, 10, 0);

		// Assert
		Assert.AreEqual(10, result);
	}

	[TestMethod]
	public void VatShare_FivePercent_SplitsGrossCorrectly()
	{
		// Arrange: 105 gross @5% -> 5 VAT
		// Act
		var result = VatSplit.VatShare(105, 5, 0);

		// Assert
		Assert.AreEqual(5, result);
	}

	[TestMethod]
	public void VatShare_ZeroPercent_ReturnsZero()
	{
		// Act
		var result = VatSplit.VatShare(100, 0, 0);

		// Assert
		Assert.AreEqual(0, result);
	}

	[TestMethod]
	public void VatShare_RoundsAwayFromZero()
	{
		// Arrange: 100 gross @10% -> 9.0909... VAT, rounded to 2 digits
		// Act
		var result = VatSplit.VatShare(100, 10, 2);

		// Assert
		Assert.AreEqual(9.09m, result);
	}
}
