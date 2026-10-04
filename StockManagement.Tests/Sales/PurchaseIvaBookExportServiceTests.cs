using Moq;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Sales.Core;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class PurchaseIvaBookExportServiceTests
{
	private readonly Mock<IGoodsImportDocumentServiceProvider> _goodsImportDocuments = new();
	private readonly Mock<ISettingsService> _settings = new();

	private readonly DateTime _from = new(2026, 9, 1);
	private readonly DateTime _to = new(2026, 9, 30);


	[TestInitialize]
	public void Initialize()
	{
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("Acme", "1", "PYG", 10, 30, 1, 1001, 0));
	}

	[TestMethod]
	public async Task GetRowsAsync_GoodsImportDocument_MapsNumberDateSupplierAndRatesFromPurchasePrice()
	{
		// Arrange
		var supplier = new Supplier("Globex");
		var document = new GoodsImportDocument
		{
			ProformaNumber = "PRF-001",
			Date = new DateTime(2026, 9, 15),
			Supplier = supplier,
			Items = [new GoodsImportDocumentItem(new StockItem("Soda") { PurchasePrice = 100, PurchaseExchangeRate = 1, VatRatePercent = 10 }) { Amount = 1 }]
		};
		this.SetupPage(document);

		// Act
		var result = await this.CreateService().GetRowsAsync(_from, _to);

		// Assert
		Assert.AreEqual(1, result.Count);
		var row = result[0];
		Assert.AreEqual("1", row.DocumentTypeCode);
		Assert.AreEqual("PRF-001", row.Number);
		Assert.AreEqual(document.Date, row.Date);
		Assert.AreEqual("", row.SupplierRuc);
		Assert.AreEqual("Globex", row.SupplierName);
		Assert.AreEqual(91, row.Taxed10);
		Assert.AreEqual(9, row.Vat10);
		Assert.AreEqual(100, row.Total);
	}

	[TestMethod]
	public async Task GetRowsAsync_UsesPurchasePriceTimesExchangeRate_NotSalePrice()
	{
		// Arrange
		var document = new GoodsImportDocument
		{
			ProformaNumber = "PRF-002",
			Date = new DateTime(2026, 9, 10),
			Supplier = new Supplier("Globex"),
			Items = [new GoodsImportDocumentItem(new StockItem("Widget", price: 999) { PurchasePrice = 10, PurchaseExchangeRate = 5, VatRatePercent = 0, Amount = 50 }) { Amount = 2 }]
		};
		this.SetupPage(document);

		// Act
		var result = await this.CreateService().GetRowsAsync(_from, _to);

		// Assert
		Assert.AreEqual(100, result[0].Exempt);
		Assert.AreEqual(100, result[0].Total);
	}

	[TestMethod]
	public async Task GetRowsAsync_DocumentOutsideRange_IsExcluded()
	{
		// Arrange
		var document = new GoodsImportDocument { ProformaNumber = "PRF-003", Date = _to.AddDays(1), Supplier = new Supplier("Globex"), Items = [] };
		this.SetupPage(document);

		// Act
		var result = await this.CreateService().GetRowsAsync(_from, _to);

		// Assert
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task GetRowsAsync_PagedDocuments_FollowsCursorUntilLastPage()
	{
		// Arrange
		var first = new GoodsImportDocument { ProformaNumber = "1", Date = new DateTime(2026, 9, 5), Supplier = new Supplier("A"), Items = [] };
		var second = new GoodsImportDocument { ProformaNumber = "2", Date = new DateTime(2026, 9, 6), Supplier = new Supplier("B"), Items = [] };
		_goodsImportDocuments.Setup(provider => provider.GetGoodsImportDocumentsAsync(null, 100, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CursorPage<GoodsImportDocument>([first], "cursor-2"));
		_goodsImportDocuments.Setup(provider => provider.GetGoodsImportDocumentsAsync("cursor-2", 100, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CursorPage<GoodsImportDocument>([second], null));

		// Act
		var result = await this.CreateService().GetRowsAsync(_from, _to);

		// Assert
		Assert.AreEqual(2, result.Count);
		CollectionAssert.AreEquivalent(new[] { "1", "2" }, result.Select(row => row.Number).ToList());
	}

	private void SetupPage(params GoodsImportDocument[] documents)
	{
		_goodsImportDocuments.Setup(provider => provider.GetGoodsImportDocumentsAsync(null, 100, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CursorPage<GoodsImportDocument>(documents, null));
	}

	private PurchaseIvaBookExportService CreateService()
	{
		return new PurchaseIvaBookExportService(_goodsImportDocuments.Object, _settings.Object);
	}
}
