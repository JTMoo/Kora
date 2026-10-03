using Moq;
using StockManagement.Sales.Core;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class SaleServiceTests
{
	private readonly Mock<IStockItemServiceProvider> _stockItems = new();
	private readonly Mock<IInvoiceServiceProvider> _invoices = new();
	private readonly Mock<ISettingsService> _settings = new();
	private readonly Mock<IContingencyCdcIssuer> _contingencyCdcIssuer = new();


	[TestInitialize]
	public void Initialize()
	{
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0));
		_contingencyCdcIssuer.Setup(issuer => issuer.TryIssueAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
	}

	[TestMethod]
	public async Task CreateInvoiceAsync_CartWithItems_FillsTotalTaxAndDates()
	{
		// Arrange
		var service = this.CreateService();
		var customer = new Customer();
		var date = new DateTime(2026, 9, 1);
		List<ShoppingCartItem> items =
		[
			CreateCartItem("A1", "Screw", inStock: 10, price: 5000, quantity: 2, vatRatePercent: 10m),
			CreateCartItem("B2", "Nut", inStock: 10, price: 1000, quantity: 1, vatRatePercent: 10m)
		];

		// Act
		var invoice = await service.CreateInvoiceAsync(customer, items, date);

		// Assert
		Assert.AreSame(customer, invoice.Customer);
		Assert.AreEqual(11000, invoice.Total);
		Assert.AreEqual(1000, invoice.Tax);
		Assert.AreEqual(date, invoice.Date);
		Assert.AreEqual(date.AddDays(30), invoice.ExpirationDate);
		Assert.AreEqual("", invoice.Number);
		CollectionAssert.AreEqual(items, invoice.Items);
	}

	[TestMethod]
	public async Task CreateInvoiceAsync_Always_LeavesCdcEmpty()
	{
		// Arrange
		var service = this.CreateService();
		List<ShoppingCartItem> items = [CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 1)];

		// Act
		var invoice = await service.CreateInvoiceAsync(new Customer(), items, DateTime.Today);

		// Assert
		Assert.AreEqual("", invoice.Cdc);
	}

	[TestMethod]
	public async Task CreateInvoiceAsync_ConfiguredPaymentTerm_UsesIt()
	{
		// Arrange
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 5m, 14, 1, 1001, 0));
		var service = this.CreateService();
		var date = new DateTime(2026, 9, 1);
		List<ShoppingCartItem> items = [CreateCartItem("A1", "Screw", inStock: 10, price: 2100, quantity: 1, vatRatePercent: 5m)];

		// Act
		var invoice = await service.CreateInvoiceAsync(new Customer(), items, date);

		// Assert
		Assert.AreEqual(100, invoice.Tax);
		Assert.AreEqual(date.AddDays(14), invoice.ExpirationDate);
	}

	[TestMethod]
	public async Task CreateInvoiceAsync_MixedVatRateItems_TaxesEachLineAtItsOwnRate()
	{
		// Arrange
		var service = this.CreateService();
		var date = new DateTime(2026, 9, 1);
		List<ShoppingCartItem> items =
		[
			CreateCartItem("A1", "Soda", inStock: 10, price: 110, quantity: 1, vatRatePercent: 10m),
			CreateCartItem("B2", "Milk", inStock: 10, price: 105, quantity: 1, vatRatePercent: 5m)
		];

		// Act
		var invoice = await service.CreateInvoiceAsync(new Customer(), items, date);

		// Assert
		Assert.AreEqual(215, invoice.Total);
		Assert.AreEqual(15, invoice.Tax);
	}

	[TestMethod]
	public void CalculateTotal_CartItems_UsesRoundedUnitPrices()
	{
		// Arrange
		List<ShoppingCartItem> items =
		[
			CreateCartItem("A1", "Screw", inStock: 10, price: 5000, quantity: 2),
			new ShoppingCartItem(new StockItem("Nut", code: "B2", amount: 10) { Price = 99.6m }) { Amount = 1 }
		];

		// Act
		var result = this.CreateService().CalculateTotal(items, 0);

		// Assert
		Assert.AreEqual(10100, result);
	}

	[TestMethod]
	public async Task GetNextInvoiceNumberAsync_NoInvoices_ReturnsOne()
	{
		// Arrange
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

		// Act
		var result = await this.CreateService().GetNextInvoiceNumberAsync();

		// Assert
		Assert.AreEqual("001-001-0000001", result);
	}

	[TestMethod]
	public async Task GetNextInvoiceNumberAsync_ExistingInvoices_ReturnsHighestPlusOne()
	{
		// Arrange
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new Invoice() { Number = "001-001-0000003" }, new Invoice() { Number = "001-001-0000041" }]);

		// Act
		var result = await this.CreateService().GetNextInvoiceNumberAsync();

		// Assert
		Assert.AreEqual("001-001-0000042", result);
	}

	[TestMethod]
	public async Task GetNextInvoiceNumberAsync_InvoicesFromAnotherPointOfSale_IgnoresThem()
	{
		// Arrange
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0, EstablishmentCode: "002", PointOfSaleCode: "001"));
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new Invoice() { Number = "001-001-0000099" }]);

		// Act
		var result = await this.CreateService().GetNextInvoiceNumberAsync();

		// Assert
		Assert.AreEqual("002-001-0000001", result);
	}

	[TestMethod]
	public async Task CompleteSaleAsync_EnoughStock_ReducesStockAndStoresInvoice()
	{
		// Arrange
		var stored = new StockItem("Screw", code: "A1", amount: 10);
		this.SetupStock(stored);
		var invoice = CreateInvoice(CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 3));
		var service = this.CreateService();

		// Act
		var result = await service.CompleteSaleAsync(invoice);

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual(7, stored.Amount);
		Assert.AreEqual(7, invoice.Items[0].StockItem.Amount);
		_invoices.Verify(provider => provider.TryAddSaleAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task CompleteSaleAsync_StockDroppedSinceCartWasFilled_WritesNothing()
	{
		// Arrange
		this.SetupStock(new StockItem("Screw", code: "A1", amount: 2), new StockItem("Nut", code: "B2", amount: 10));
		var invoice = CreateInvoice(
			CreateCartItem("B2", "Nut", inStock: 10, price: 100, quantity: 1),
			CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 3));
		var service = this.CreateService();

		// Act
		var result = await service.CompleteSaleAsync(invoice);

		// Assert
		Assert.IsFalse(result.Succeeded);
		CollectionAssert.AreEqual(new[] { "Screw" }, result.UnavailableItems.ToList());
		_invoices.Verify(provider => provider.TryAddSaleAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CompleteSaleAsync_StockShortage_NeverReservesCdc()
	{
		// Arrange (#167): shortage must short-circuit before the CDC is reserved
		this.SetupStock(new StockItem("Screw", code: "A1", amount: 2));
		var invoice = CreateInvoice(CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 3));
		var service = this.CreateService();

		// Act
		await service.CompleteSaleAsync(invoice);

		// Assert
		_contingencyCdcIssuer.Verify(issuer => issuer.TryIssueAsync(It.IsAny<CancellationToken>()), Times.Never);
		Assert.AreEqual("", invoice.Cdc);
	}

	[TestMethod]
	public async Task CompleteSaleAsync_ContingencyModeOff_LeavesCdcEmpty()
	{
		// Arrange
		this.SetupStock(new StockItem("Screw", code: "A1", amount: 10));
		var invoice = CreateInvoice(CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 1));
		var service = this.CreateService();

		// Act
		await service.CompleteSaleAsync(invoice);

		// Assert
		Assert.AreEqual("", invoice.Cdc);
	}

	[TestMethod]
	public async Task CompleteSaleAsync_ContingencyModeActive_IssuesCdcLocallyAfterShortageCheck()
	{
		// Arrange
		var cdc = "0" + new string('1', 43);
		_contingencyCdcIssuer.Setup(issuer => issuer.TryIssueAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cdc);
		this.SetupStock(new StockItem("Screw", code: "A1", amount: 10));
		var invoice = CreateInvoice(CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 1));
		var service = this.CreateService();

		// Act
		var result = await service.CompleteSaleAsync(invoice);

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual(cdc, invoice.Cdc);
	}

	[TestMethod]
	public async Task CompleteSaleAsync_StockTakenBetweenCheckAndWrite_ReportsWriteShortage()
	{
		// Arrange
		this.SetupStock(new StockItem("Screw", code: "A1", amount: 10));
		var invoice = CreateInvoice(CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 3));
		_invoices.Setup(provider => provider.TryAddSaleAsync(invoice, It.IsAny<CancellationToken>())).ReturnsAsync(["Screw"]);

		// Act
		var result = await this.CreateService().CompleteSaleAsync(invoice);

		// Assert
		Assert.IsFalse(result.Succeeded);
		CollectionAssert.AreEqual(new[] { "Screw" }, result.UnavailableItems.ToList());
	}

	[TestMethod]
	public async Task CompleteSaleAsync_ArticleDeletedMeanwhile_ReportsItUnavailable()
	{
		// Arrange
		var invoice = CreateInvoice(CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 1));
		var service = this.CreateService();

		// Act
		var result = await service.CompleteSaleAsync(invoice);

		// Assert
		Assert.IsFalse(result.Succeeded);
		CollectionAssert.AreEqual(new[] { "Screw" }, result.UnavailableItems.ToList());
		_invoices.Verify(provider => provider.TryAddSaleAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CompleteSaleAsync_Cancelled_WritesNothing()
	{
		// Arrange
		this.SetupStock(new StockItem("Screw", code: "A1", amount: 10));
		var invoice = CreateInvoice(CreateCartItem("A1", "Screw", inStock: 10, price: 100, quantity: 1));
		var service = this.CreateService();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		// Act
		await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => service.CompleteSaleAsync(invoice, cancellation.Token));

		// Assert
		_invoices.Verify(provider => provider.TryAddSaleAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}


	[TestMethod]
	public async Task SellAsync_EnoughStock_StoresNumberedInvoice()
	{
		// Arrange
		var stored = new StockItem("Screw", code: "A1", amount: 10, price: 5000);
		this.SetupStock(stored);
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new Invoice() { Number = "001-001-0000007" }]);
		var customer = new Customer() { CustomerId = 1001 };
		var date = new DateTime(2026, 9, 1);

		// Act
		var result = await this.CreateService().SellAsync(customer, [new SaleItem("A1", 3)], SaleCondition.Cash, date);

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual("001-001-0000008", result.Invoice.Number);
		Assert.AreEqual(15000, result.Invoice.Total);
		Assert.AreEqual(SaleCondition.Cash, result.Invoice.SaleCondition);
		Assert.AreSame(customer, result.Invoice.Customer);
		Assert.AreEqual(7, stored.Amount);
		_invoices.Verify(provider => provider.TryAddSaleAsync(result.Invoice, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task SellAsync_MoreThanInStock_ReportsShortageAndWritesNothing()
	{
		// Arrange
		this.SetupStock(new StockItem("Screw", code: "A1", amount: 2));

		// Act
		var result = await this.CreateService().SellAsync(new Customer(), [new SaleItem("A1", 3)], SaleCondition.Cash, DateTime.Today);

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.IsNull(result.Invoice);
		CollectionAssert.AreEqual(new[] { "Screw" }, result.UnavailableItems.ToList());
		_invoices.Verify(provider => provider.TryAddSaleAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task SellAsync_SameCodeTwiceAboveStock_ReportsShortage()
	{
		// Arrange
		this.SetupStock(new StockItem("Screw", code: "A1", amount: 4));

		// Act
		var result = await this.CreateService().SellAsync(new Customer(), [new SaleItem("A1", 2), new SaleItem("A1", 3)], SaleCondition.Cash, DateTime.Today);

		// Assert
		CollectionAssert.AreEqual(new[] { "Screw" }, result.UnavailableItems.ToList());
	}

	[TestMethod]
	public async Task SellAsync_UnknownCode_ReportsCodeUnavailable()
	{
		// Act
		var result = await this.CreateService().SellAsync(new Customer(), [new SaleItem("X9", 1)], SaleCondition.Cash, DateTime.Today);

		// Assert
		CollectionAssert.AreEqual(new[] { "X9" }, result.UnavailableItems.ToList());
		_invoices.Verify(provider => provider.TryAddSaleAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task SellAsync_ZeroAmount_Throws()
	{
		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => this.CreateService().SellAsync(new Customer(), [new SaleItem("A1", 0)], SaleCondition.Cash, DateTime.Today));
	}

	private SaleService CreateService()
	{
		return new SaleService(_stockItems.Object, _invoices.Object, _settings.Object, _contingencyCdcIssuer.Object);
	}

	/// <remarks>The sale write takes the lines out of <paramref name="stockItems"/>, like the transaction in the database.</remarks>
	private void SetupStock(params StockItem[] stockItems)
	{
		foreach (var stockItem in stockItems)
		{
			_stockItems.Setup(provider => provider.GetStockItemAsync(stockItem.Code, It.IsAny<CancellationToken>())).ReturnsAsync(stockItem);
		}

		_invoices
			.Setup(provider => provider.TryAddSaleAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Invoice invoice, CancellationToken _) =>
			{
				foreach (var item in invoice.Items)
				{
					var stored = stockItems.Single(stockItem => stockItem.Code == item.StockItem.Code);
					stored.Amount -= item.Amount;
					item.StockItem.Amount = stored.Amount;
				}

				return [];
			});
	}

	private static ShoppingCartItem CreateCartItem(string code, string name, int inStock, int price, int quantity, decimal vatRatePercent = 0)
	{
		return new ShoppingCartItem(new StockItem(name, code: code, amount: inStock, price: price) { VatRatePercent = vatRatePercent }) { Amount = quantity };
	}

	private static Invoice CreateInvoice(params ShoppingCartItem[] items)
	{
		return new Invoice() { Items = [.. items] };
	}
}
