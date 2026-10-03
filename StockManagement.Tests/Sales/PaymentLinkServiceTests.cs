using Moq;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class PaymentLinkServiceTests
{
	private readonly Mock<IInvoiceServiceProvider> _invoices = new();
	private readonly Mock<IPaymentLinkServiceProvider> _paymentLinks = new();
	private readonly Mock<IPaymentLinkGateway> _gateway = new();
	private readonly Mock<ISettingsService> _settings = new();

	[TestInitialize]
	public void Initialize()
	{
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0));
	}

	[TestMethod]
	public async Task CreateForInvoiceAsync_InvoiceNotFound_ReturnsFailure()
	{
		// Act
		var result = await this.CreateService().CreateForInvoiceAsync("42");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(CreatePaymentLinkError.InvoiceNotFound, result.Error);
	}

	[TestMethod]
	public async Task CreateForInvoiceAsync_InvoiceFullyPaid_ReturnsFailure()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 1000, Payments = [new Payment { Amount = 1000 }] };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

		// Act
		var result = await this.CreateService().CreateForInvoiceAsync("1");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(CreatePaymentLinkError.AlreadyPaid, result.Error);
		_gateway.Verify(gateway => gateway.CreateAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CreateForInvoiceAsync_GatewayRejects_ReturnsFailure()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		_gateway.Setup(gateway => gateway.CreateAsync("1", 1000, It.IsAny<CancellationToken>())).ReturnsAsync(PaymentLinkGatewayResult.Failure("bancard down"));

		// Act
		var result = await this.CreateService().CreateForInvoiceAsync("1");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(CreatePaymentLinkError.GatewayError, result.Error);
		_paymentLinks.Verify(provider => provider.AddAsync(It.IsAny<PaymentLink>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CreateForInvoiceAsync_GatewaySucceeds_PersistsPendingLinkForAmountDue()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 1000, Payments = [new Payment { Amount = 400 }] };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		_gateway.Setup(gateway => gateway.CreateAsync("1", 600, It.IsAny<CancellationToken>())).ReturnsAsync(PaymentLinkGatewayResult.Success("ext-1", "https://bancard/qr/ext-1"));

		// Act
		var result = await this.CreateService().CreateForInvoiceAsync("1");

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual("ext-1", result.Link!.ExternalId);
		Assert.AreEqual("https://bancard/qr/ext-1", result.Link.QrUrl);
		Assert.AreEqual(600, result.Link.Amount);
		Assert.AreEqual(PaymentLinkStatus.Pending, result.Link.Status);
		_paymentLinks.Verify(provider => provider.AddAsync(result.Link, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task ConfirmAsync_UnknownExternalId_ReturnsFalse()
	{
		// Act
		var result = await this.CreateService().ConfirmAsync("missing");

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public async Task ConfirmAsync_AlreadyPaid_DoesNotQueryGatewayAgain()
	{
		// Arrange
		var link = new PaymentLink { Invoice = new Invoice { Number = "1" }, ExternalId = "ext-1", Status = PaymentLinkStatus.Paid };
		_paymentLinks.Setup(provider => provider.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(link);

		// Act
		var result = await this.CreateService().ConfirmAsync("ext-1");

		// Assert
		Assert.IsTrue(result);
		_gateway.Verify(gateway => gateway.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task ConfirmAsync_GatewayReportsPaid_RecordsPaymentAndMarksLinkPaid()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 600 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var link = new PaymentLink { Invoice = invoice, ExternalId = "ext-1", Amount = 600, Status = PaymentLinkStatus.Pending, ExpiresAt = DateTime.Now.AddHours(1) };
		_paymentLinks.Setup(provider => provider.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(link);
		_gateway.Setup(gateway => gateway.GetStatusAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(PaymentLinkStatus.Paid);

		// Act
		var result = await this.CreateService().ConfirmAsync("ext-1");

		// Assert
		Assert.IsTrue(result);
		Assert.AreEqual(PaymentLinkStatus.Paid, link.Status);
		Assert.IsNotNull(link.PaidAt);
		Assert.AreEqual(1, invoice.Payments.Count);
		Assert.AreEqual(PaymentMethod.BancardQr, invoice.Payments[0].Method);
		_paymentLinks.Verify(provider => provider.UpdateAsync(link, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task ConfirmAsync_GatewayReportsExpired_UpdatesLinkStatusWithoutRecordingPayment()
	{
		// Arrange
		var invoice = new Invoice { Number = "1", Total = 600 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var link = new PaymentLink { Invoice = invoice, ExternalId = "ext-1", Amount = 600, Status = PaymentLinkStatus.Pending, ExpiresAt = DateTime.Now.AddHours(1) };
		_paymentLinks.Setup(provider => provider.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(link);
		_gateway.Setup(gateway => gateway.GetStatusAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(PaymentLinkStatus.Expired);

		// Act
		var result = await this.CreateService().ConfirmAsync("ext-1");

		// Assert
		Assert.IsTrue(result);
		Assert.AreEqual(PaymentLinkStatus.Expired, link.Status);
		Assert.AreEqual(0, invoice.Payments.Count);
	}

	[TestMethod]
	public async Task ConfirmAsync_LinkPastExpiresAt_MarksExpiredWithoutQueryingGateway()
	{
		// Arrange (#169)
		var link = new PaymentLink { Invoice = new Invoice { Number = "1" }, ExternalId = "ext-1", Status = PaymentLinkStatus.Pending, ExpiresAt = DateTime.Now.AddMinutes(-1) };
		_paymentLinks.Setup(provider => provider.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(link);

		// Act
		var result = await this.CreateService().ConfirmAsync("ext-1");

		// Assert
		Assert.IsTrue(result);
		Assert.AreEqual(PaymentLinkStatus.Expired, link.Status);
		_gateway.Verify(gateway => gateway.GetStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
		_paymentLinks.Verify(provider => provider.UpdateAsync(link, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task CreateForInvoiceAsync_LiveLinkAlreadyPending_ReturnsItWithoutMintingAnother()
	{
		// Arrange (#169)
		var invoice = new Invoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var existing = new PaymentLink { Invoice = invoice, ExternalId = "ext-1", QrUrl = "https://bancard/qr/ext-1", Amount = 1000, Status = PaymentLinkStatus.Pending, ExpiresAt = DateTime.Now.AddMinutes(30) };
		_paymentLinks.Setup(provider => provider.GetLatestForInvoiceAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

		// Act
		var result = await this.CreateService().CreateForInvoiceAsync("1");

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreSame(existing, result.Link);
		_gateway.Verify(gateway => gateway.CreateAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
		_paymentLinks.Verify(provider => provider.AddAsync(It.IsAny<PaymentLink>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CreateForInvoiceAsync_PendingLinkExpired_ExpiresItAndMintsANewOne()
	{
		// Arrange (#169)
		var invoice = new Invoice { Number = "1", Total = 1000 };
		_invoices.Setup(provider => provider.GetInvoiceAync("1", It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var stale = new PaymentLink { Invoice = invoice, ExternalId = "ext-stale", Status = PaymentLinkStatus.Pending, ExpiresAt = DateTime.Now.AddMinutes(-1) };
		_paymentLinks.Setup(provider => provider.GetLatestForInvoiceAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(stale);
		_gateway.Setup(gateway => gateway.CreateAsync("1", 1000, It.IsAny<CancellationToken>())).ReturnsAsync(PaymentLinkGatewayResult.Success("ext-new", "https://bancard/qr/ext-new"));

		// Act
		var result = await this.CreateService().CreateForInvoiceAsync("1");

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual("ext-new", result.Link!.ExternalId);
		Assert.AreEqual(PaymentLinkStatus.Expired, stale.Status);
		_paymentLinks.Verify(provider => provider.UpdateAsync(stale, It.IsAny<CancellationToken>()), Times.Once);
		_paymentLinks.Verify(provider => provider.AddAsync(result.Link, It.IsAny<CancellationToken>()), Times.Once);
	}

	private PaymentLinkService CreateService()
	{
		return new PaymentLinkService(_invoices.Object, _paymentLinks.Object, _gateway.Object, new PaymentService(_invoices.Object, _settings.Object));
	}
}
