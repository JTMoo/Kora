using Moq;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class SupplierPaymentServiceTests
{
	private readonly Mock<ISupplierInvoiceServiceProvider> _invoices = new();
	private readonly Mock<ISettingsService> _settings = new();


	[TestInitialize]
	public void Initialize()
	{
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0));
	}

	[TestMethod]
	public async Task RecordPaymentAsync_InvoiceNotFound_ReturnsFailure()
	{
		// Act
		var result = await this.CreateService().RecordPaymentAsync("42", 100, PaymentMethod.Cash, DateTime.Now);

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(RecordSupplierPaymentError.SupplierInvoiceNotFound, result.Error);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_ZeroAmount_ReturnsFailure()
	{
		// Arrange
		var invoice = new SupplierInvoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetSupplierInvoiceAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 0, PaymentMethod.Cash, DateTime.Now);

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(RecordSupplierPaymentError.InvalidAmount, result.Error);
		_invoices.Verify(provider => provider.UpdateSupplierInvoiceAsync(It.IsAny<SupplierInvoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_AmountAboveAmountDue_ReturnsFailure()
	{
		// Arrange
		var invoice = new SupplierInvoice { Number = "1", Total = 1000, Payments = [new Payment { Amount = 900 }] };
		_invoices.Setup(provider => provider.GetSupplierInvoiceAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 200, PaymentMethod.Cash, DateTime.Now);

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(RecordSupplierPaymentError.ExceedsAmountDue, result.Error);
		_invoices.Verify(provider => provider.UpdateSupplierInvoiceAsync(It.IsAny<SupplierInvoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_ValidAmount_AddsPaymentAndUpdatesInvoice()
	{
		// Arrange
		var invoice = new SupplierInvoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetSupplierInvoiceAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var date = new DateTime(2026, 9, 1);

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 400, PaymentMethod.BankTransfer, date);

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual(400, result.Payment!.Amount);
		Assert.AreEqual(PaymentMethod.BankTransfer, result.Payment.Method);
		Assert.AreEqual(date, result.Payment.Date);
		CollectionAssert.Contains(invoice.Payments, result.Payment);
		_invoices.Verify(provider => provider.UpdateSupplierInvoiceAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public void GetStatus_AmountDueZero_ReturnsPaid()
	{
		// Arrange
		var invoice = new SupplierInvoice { Total = 100, ExpirationDate = DateTime.Now.AddDays(5), Payments = [new Payment { Amount = 100 }] };

		// Act
		var status = this.CreateService().GetStatus(invoice, DateTime.Now);

		// Assert
		Assert.AreEqual(SupplierInvoiceStatus.Paid, status);
	}

	[TestMethod]
	public void GetStatus_PastExpirationWithAmountDue_ReturnsOverdue()
	{
		// Arrange
		var invoice = new SupplierInvoice { Total = 100, ExpirationDate = DateTime.Now.AddDays(-1) };

		// Act
		var status = this.CreateService().GetStatus(invoice, DateTime.Now);

		// Assert
		Assert.AreEqual(SupplierInvoiceStatus.Overdue, status);
	}

	[TestMethod]
	public async Task GetOpenSupplierInvoicesAsync_FiltersOutFullyPaidAndOrdersBySoonestDue()
	{
		// Arrange
		var now = DateTime.Now;
		var paid = new SupplierInvoice { Number = "1", Total = 100, ExpirationDate = now.AddDays(5), Payments = [new Payment { Amount = 100 }] };
		var dueSoon = new SupplierInvoice { Number = "2", Total = 100, ExpirationDate = now.AddDays(2) };
		var dueLater = new SupplierInvoice { Number = "3", Total = 100, ExpirationDate = now.AddDays(20) };
		_invoices.Setup(provider => provider.GetSupplierInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([paid, dueLater, dueSoon]);

		// Act
		var result = await this.CreateService().GetOpenSupplierInvoicesAsync(supplierId: null, cursor: null, pageSize: 20);

		// Assert
		Assert.AreEqual(2, result.Items.Count);
		CollectionAssert.AreEqual(new[] { dueSoon, dueLater }, result.Items.ToList());
	}

	[TestMethod]
	public async Task GetOpenSupplierInvoicesAsync_FiltersBySupplier()
	{
		// Arrange
		var supplierA = new Supplier { Id = "a" };
		var supplierB = new Supplier { Id = "b" };
		var invoiceA = new SupplierInvoice { Number = "1", Total = 100, ExpirationDate = DateTime.Now.AddDays(5), Supplier = supplierA };
		var invoiceB = new SupplierInvoice { Number = "2", Total = 100, ExpirationDate = DateTime.Now.AddDays(5), Supplier = supplierB };
		_invoices.Setup(provider => provider.GetSupplierInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([invoiceA, invoiceB]);

		// Act
		var result = await this.CreateService().GetOpenSupplierInvoicesAsync(supplierId: "a", cursor: null, pageSize: 20);

		// Assert
		CollectionAssert.AreEqual(new[] { invoiceA }, result.Items.ToList());
	}

	private SupplierPaymentService CreateService()
	{
		return new SupplierPaymentService(_invoices.Object, _settings.Object);
	}
}
