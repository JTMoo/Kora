using Moq;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Sales.Core;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class DebitNoteServiceTests
{
	private readonly Mock<IDebitNoteServiceProvider> _debitNotes = new();
	private readonly Mock<ISettingsService> _settings = new();
	private readonly Mock<IContingencyCdcIssuer> _contingencyCdcIssuer = new();


	[TestInitialize]
	public void Initialize()
	{
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0));
		_debitNotes.Setup(provider => provider.GetDebitNotesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		_contingencyCdcIssuer.Setup(issuer => issuer.TryIssueAsync(It.IsAny<SifenDocumentType>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
	}

	private static Invoice TransmittedInvoice() => new() { Number = "001-001-0000050", Cdc = new string('1', 44) };

	[TestMethod]
	public async Task GetNextNumberAsync_NoDebitNotesYet_UsesConfiguredFirstNumber()
	{
		// Arrange
		var service = this.CreateService();

		// Act
		var number = await service.GetNextNumberAsync();

		// Assert
		Assert.AreEqual("001-001-0000001", number);
	}

	[TestMethod]
	public async Task GetNextNumberAsync_ExistingDebitNotes_IsHighestPlusOne()
	{
		// Arrange
		_debitNotes.Setup(provider => provider.GetDebitNotesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([new DebitNote { Number = "001-001-0000005" }]);
		var service = this.CreateService();

		// Act
		var number = await service.GetNextNumberAsync();

		// Assert
		Assert.AreEqual("001-001-0000006", number);
	}

	[TestMethod]
	public async Task CreateAsync_ValidItems_StoresNumberedDebitNoteViaProvider()
	{
		// Arrange
		var service = this.CreateService();
		var invoice = TransmittedInvoice();
		var date = new DateTime(2026, 10, 4);

		// Act
		var debitNote = await service.CreateAsync(invoice, "Intereses por mora", [("Intereses por mora", 11000m, 10)], date);

		// Assert
		Assert.AreSame(invoice, debitNote.Invoice);
		Assert.AreEqual("Intereses por mora", debitNote.Reason);
		Assert.AreEqual("001-001-0000001", debitNote.Number);
		Assert.AreEqual(1, debitNote.Items.Count);
		Assert.AreEqual(11000m, debitNote.Total);
		Assert.AreEqual(1000m, debitNote.Tax);
		Assert.AreEqual("", debitNote.Cdc);
		_debitNotes.Verify(provider => provider.AddAsync(debitNote, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task CreateAsync_ContingencyModeActive_IssuesCdcLocally()
	{
		// Arrange
		var cdc = "0" + new string('7', 43);
		_contingencyCdcIssuer.Setup(issuer => issuer.TryIssueAsync(SifenDocumentType.NotaDeDebitoElectronica, It.IsAny<CancellationToken>())).ReturnsAsync(cdc);
		var service = this.CreateService();

		// Act
		var debitNote = await service.CreateAsync(TransmittedInvoice(), "reason", [("line", 1000m, 10)], DateTime.Today);

		// Assert
		Assert.AreEqual(cdc, debitNote.Cdc);
	}

	[TestMethod]
	public async Task CreateAsync_InvoiceNotTransmitted_Throws()
	{
		// Arrange: a debit note can't reference a DE SIFEN never saw
		var service = this.CreateService();
		var invoice = new Invoice { Number = "001-001-0000050", Cdc = "" };

		// Act + Assert
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.CreateAsync(invoice, "reason", [("line", 1000m, 10)], DateTime.Today));
		_contingencyCdcIssuer.Verify(issuer => issuer.TryIssueAsync(It.IsAny<SifenDocumentType>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CreateAsync_NoItems_NeverReservesCdc()
	{
		// Arrange (#167's reasoning applied here too): a rejected debit note must never burn a reserved contingency number
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.CreateAsync(TransmittedInvoice(), "reason", [], DateTime.Today));
		_contingencyCdcIssuer.Verify(issuer => issuer.TryIssueAsync(It.IsAny<SifenDocumentType>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CreateAsync_NonPositiveAmount_Throws()
	{
		// Arrange
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => service.CreateAsync(TransmittedInvoice(), "reason", [("line", 0m, 10)], DateTime.Today));
	}

	private DebitNoteService CreateService()
	{
		return new DebitNoteService(_debitNotes.Object, _settings.Object, _contingencyCdcIssuer.Object);
	}
}
