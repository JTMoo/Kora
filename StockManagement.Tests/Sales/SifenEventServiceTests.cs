using Moq;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class SifenEventServiceTests
{
	private readonly Mock<IInvoiceServiceProvider> _invoices = new();
	private readonly Mock<ICancellationRequestServiceProvider> _cancellationRequests = new();
	private readonly Mock<IInvoiceNumberVoidServiceProvider> _numberVoids = new();
	private readonly Mock<ISettingsService> _settings = new();

	private static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0);


	[TestInitialize]
	public void Initialize()
	{
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0));
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<Invoice>());
		_numberVoids.Setup(provider => provider.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
	}

	private SifenEventService CreateService() => new(_invoices.Object, _cancellationRequests.Object, _numberVoids.Object, _settings.Object);

	private static Invoice AcceptedInvoice(DateTime? acceptedAt = null) => new()
	{
		Number = "001-001-0000050",
		Cdc = new string('1', 44),
		TransmissionStatus = TransmissionStatus.Accepted,
		AcceptedAt = acceptedAt ?? Now,
	};

	#region RequestCancellationAsync

	[TestMethod]
	public async Task RequestCancellationAsync_AcceptedInvoiceWithinWindow_QueuesRequest()
	{
		// Arrange
		var invoice = AcceptedInvoice(acceptedAt: Now.AddHours(-1));
		_invoices.Setup(provider => provider.GetInvoiceAync(invoice.Number, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var service = this.CreateService();

		// Act
		var request = await service.RequestCancellationAsync(invoice.Number, "cancelled by mistake", Now);

		// Assert
		Assert.AreSame(invoice, request.Invoice);
		Assert.AreEqual("cancelled by mistake", request.Reason);
		_cancellationRequests.Verify(provider => provider.AddAsync(request, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task RequestCancellationAsync_EmptyReason_Throws()
	{
		// Arrange
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.RequestCancellationAsync("001-001-0000050", "", Now));
	}

	[TestMethod]
	public async Task RequestCancellationAsync_InvoiceDoesNotExist_Throws()
	{
		// Arrange
		_invoices.Setup(provider => provider.GetInvoiceAync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Invoice)null!);
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.RequestCancellationAsync("001-001-9999999", "reason", Now));
	}

	[TestMethod]
	public async Task RequestCancellationAsync_InvoiceNotAccepted_Throws()
	{
		// Arrange: a sale that was never transmitted, or was rejected, has no SIFEN record to retract
		var invoice = new Invoice { Number = "001-001-0000050", TransmissionStatus = TransmissionStatus.Pending };
		_invoices.Setup(provider => provider.GetInvoiceAync(invoice.Number, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.RequestCancellationAsync(invoice.Number, "reason", Now));
	}

	[TestMethod]
	public async Task RequestCancellationAsync_AlreadyHasOpenCancellation_Throws()
	{
		// Arrange
		var invoice = AcceptedInvoice();
		_invoices.Setup(provider => provider.GetInvoiceAync(invoice.Number, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		_cancellationRequests.Setup(provider => provider.GetOpenForInvoiceAsync(invoice, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CancellationRequest(invoice, "earlier reason", Now.AddMinutes(-5)));
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.RequestCancellationAsync(invoice.Number, "reason", Now));
	}

	[TestMethod]
	public async Task RequestCancellationAsync_Past48hWindow_Throws()
	{
		// Arrange
		var invoice = AcceptedInvoice(acceptedAt: Now.AddHours(-49));
		_invoices.Setup(provider => provider.GetInvoiceAync(invoice.Number, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.RequestCancellationAsync(invoice.Number, "reason", Now));
		_cancellationRequests.Verify(provider => provider.AddAsync(It.IsAny<CancellationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	#endregion

	#region RequestNumberVoidAsync

	[TestMethod]
	public async Task RequestNumberVoidAsync_ValidRange_QueuesVoid()
	{
		// Arrange
		var service = this.CreateService();

		// Act
		var numberVoid = await service.RequestNumberVoidAsync(10, 20, "skipped after a crash", Now);

		// Assert
		Assert.AreEqual(10, numberVoid.RangeStart);
		Assert.AreEqual(20, numberVoid.RangeEnd);
		_numberVoids.Verify(provider => provider.AddAsync(numberVoid, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task RequestNumberVoidAsync_RangeEndBeforeStart_Throws()
	{
		// Arrange
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.RequestNumberVoidAsync(20, 10, "reason", Now));
	}

	[TestMethod]
	public async Task RequestNumberVoidAsync_MoreThan1000Numbers_Throws()
	{
		// Arrange
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.RequestNumberVoidAsync(1, 1002, "reason", Now));
	}

	[TestMethod]
	public async Task RequestNumberVoidAsync_ReasonOver150Chars_Throws()
	{
		// Arrange
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.RequestNumberVoidAsync(1, 10, new string('x', 151), Now));
	}

	[TestMethod]
	public async Task RequestNumberVoidAsync_OverlapsInvoiceNumberInUse_Throws()
	{
		// Arrange
		_invoices.Setup(provider => provider.GetInvoicesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([new Invoice { Number = "001-001-0000015" }]);
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.RequestNumberVoidAsync(10, 20, "reason", Now));
	}

	[TestMethod]
	public async Task RequestNumberVoidAsync_OverlapsExistingVoid_Throws()
	{
		// Arrange
		_numberVoids.Setup(provider => provider.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([new InvoiceNumberVoid(15, 25, "earlier", Now.AddDays(-1)) { TransmissionStatus = TransmissionStatus.Accepted }]);
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.RequestNumberVoidAsync(10, 20, "reason", Now));
	}

	[TestMethod]
	public async Task RequestNumberVoidAsync_OverlapsRejectedVoid_Allowed()
	{
		// Arrange: a rejected void never happened, so its range is still open
		_numberVoids.Setup(provider => provider.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([new InvoiceNumberVoid(15, 25, "earlier", Now.AddDays(-1)) { TransmissionStatus = TransmissionStatus.Rejected }]);
		var service = this.CreateService();

		// Act
		var numberVoid = await service.RequestNumberVoidAsync(10, 20, "reason", Now);

		// Assert
		Assert.AreEqual(10, numberVoid.RangeStart);
	}

	#endregion
}
