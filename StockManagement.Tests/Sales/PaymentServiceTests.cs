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
public sealed class PaymentServiceTests
{
	private readonly Mock<IInvoiceServiceProvider> _invoices = new();
	private readonly Mock<ISettingsService> _settings = new();
	private readonly Mock<ICashRegisterService> _cashRegister = new();


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
		Assert.AreEqual(RecordPaymentError.InvoiceNotFound, result.Error);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_ZeroAmount_ReturnsFailure()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 0, PaymentMethod.Cash, DateTime.Now);

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(RecordPaymentError.InvalidAmount, result.Error);
		_invoices.Verify(provider => provider.UpdateInvoiceAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_AmountAboveAmountDue_ReturnsFailure()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 1000, Payments = [new Payment { Amount = 900 }] };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 200, PaymentMethod.Cash, DateTime.Now);

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(RecordPaymentError.ExceedsAmountDue, result.Error);
		_invoices.Verify(provider => provider.UpdateInvoiceAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_ValidAmount_AddsPaymentAndUpdatesInvoice()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var date = new DateTime(2026, 9, 1);

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 400, PaymentMethod.BankTransfer, date);

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual(400, result.Payment!.Amount);
		Assert.AreEqual(PaymentMethod.BankTransfer, result.Payment.Method);
		Assert.AreEqual(date, result.Payment.Date);
		CollectionAssert.Contains(invoice.Payments, result.Payment);
		_invoices.Verify(provider => provider.UpdateInvoiceAsync(invoice, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_CashWithOpenSession_TagsPaymentWithSessionId()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		_cashRegister.Setup(service => service.GetOpenSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CashRegisterSession { Id = "session-1" });

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 400, PaymentMethod.Cash, DateTime.Now);

		// Assert
		Assert.AreEqual("session-1", result.Payment!.CashRegisterSessionId);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_NonCashMethod_DoesNotTagWithSessionId()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		_cashRegister.Setup(service => service.GetOpenSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CashRegisterSession { Id = "session-1" });

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 400, PaymentMethod.BankTransfer, DateTime.Now);

		// Assert
		Assert.IsNull(result.Payment!.CashRegisterSessionId);
		_cashRegister.Verify(service => service.GetOpenSessionAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task RecordPaymentAsync_AmountRoundedToCurrencyDigits_RoundsBeforeValidating()
	{
		// Arrange
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 2));
		var invoice = new Invoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

		// Act
		var result = await this.CreateService().RecordPaymentAsync("1", 100.006m, PaymentMethod.Cash, DateTime.Now);

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual(100.01m, result.Payment!.Amount);
	}

	[TestMethod]
	public async Task GetOpenInvoicesAsync_FiltersOutFullyPaidAndOrdersBySoonestDue()
	{
		// Arrange
		var now = DateTime.Now;
		var paid = new Invoice { Number = "1", Total = 100, ExpirationDate = now.AddDays(5), Payments = [new Payment { Amount = 100 }] };
		var dueSoon = new Invoice { Number = "2", Total = 100, ExpirationDate = now.AddDays(2) };
		var dueLater = new Invoice { Number = "3", Total = 100, ExpirationDate = now.AddDays(20) };
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([paid, dueLater, dueSoon]);

		// Act
		var result = await this.CreateService().GetOpenInvoicesAsync(customerId: null, cursor: null, pageSize: 20);

		// Assert
		Assert.AreEqual(2, result.Items.Count);
		CollectionAssert.AreEqual(new[] { dueSoon, dueLater }, result.Items.ToList());
	}

	[TestMethod]
	public async Task GetOpenInvoicesAsync_FiltersByCustomer()
	{
		// Arrange
		var customerA = new Customer { CustomerId = 1001 };
		var customerB = new Customer { CustomerId = 1002 };
		var invoiceA = new Invoice { Number = "1", Total = 100, ExpirationDate = DateTime.Now.AddDays(5), Customer = customerA };
		var invoiceB = new Invoice { Number = "2", Total = 100, ExpirationDate = DateTime.Now.AddDays(5), Customer = customerB };
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([invoiceA, invoiceB]);

		// Act
		var result = await this.CreateService().GetOpenInvoicesAsync(customerId: 1001, cursor: null, pageSize: 20);

		// Assert
		CollectionAssert.AreEqual(new[] { invoiceA }, result.Items.ToList());
	}

	[TestMethod]
	public async Task GetOverdueInvoicesAsync_OnlyReturnsPastDueUnpaidInvoices()
	{
		// Arrange
		var now = DateTime.Now;
		var overdue = new Invoice { Number = "1", Total = 100, ExpirationDate = now.AddDays(-5) };
		var notYetDue = new Invoice { Number = "2", Total = 100, ExpirationDate = now.AddDays(5) };
		var paidPastDue = new Invoice { Number = "3", Total = 100, ExpirationDate = now.AddDays(-5), Payments = [new Payment { Amount = 100 }] };
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([overdue, notYetDue, paidPastDue]);

		// Act
		var result = await this.CreateService().GetOverdueInvoicesAsync(cursor: null, pageSize: 20);

		// Assert
		CollectionAssert.AreEqual(new[] { overdue }, result.Items.ToList());
	}

	private PaymentService CreateService()
	{
		return new PaymentService(_invoices.Object, _settings.Object, _cashRegister.Object);
	}
}
