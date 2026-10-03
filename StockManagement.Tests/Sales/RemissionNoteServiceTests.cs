using Moq;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class RemissionNoteServiceTests
{
	private readonly Mock<IRemissionNoteServiceProvider> _remissionNotes = new();
	private readonly Mock<ISettingsService> _settings = new();
	private readonly Mock<IContingencyCdcIssuer> _contingencyCdcIssuer = new();


	[TestInitialize]
	public void Initialize()
	{
		_settings.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0));
		_remissionNotes.Setup(provider => provider.GetRemissionNotesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		_contingencyCdcIssuer.Setup(issuer => issuer.TryIssueAsync(It.IsAny<SifenDocumentType>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
	}

	[TestMethod]
	public async Task GetNextNumberAsync_NoRemissionNotesYet_UsesConfiguredFirstNumber()
	{
		// Arrange
		var service = this.CreateService();

		// Act
		var number = await service.GetNextNumberAsync();

		// Assert
		Assert.AreEqual("001-001-0000001", number);
	}

	[TestMethod]
	public async Task GetNextNumberAsync_ExistingRemissionNotes_IsHighestPlusOne()
	{
		// Arrange
		_remissionNotes.Setup(provider => provider.GetRemissionNotesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync([new RemissionNote { Number = "001-001-0000005" }]);
		var service = this.CreateService();

		// Act
		var number = await service.GetNextNumberAsync();

		// Assert
		Assert.AreEqual("001-001-0000006", number);
	}

	[TestMethod]
	public async Task CreateAsync_ValidItems_StoresNumberedRemissionNoteViaProvider()
	{
		// Arrange
		var service = this.CreateService();
		var customer = new Customer();
		var stockItem = new StockItem { Code = "A1", Name = "Screw", Amount = 10 };
		var date = new DateTime(2026, 10, 3);

		// Act
		var remissionNote = await service.CreateAsync(customer, [(stockItem, 2)], RemissionReason.Venta, "Avda. España 500", date);

		// Assert
		Assert.AreSame(customer, remissionNote.Customer);
		Assert.AreEqual(RemissionReason.Venta, remissionNote.Reason);
		Assert.AreEqual("Avda. España 500", remissionNote.DestinationAddress);
		Assert.AreEqual("001-001-0000001", remissionNote.Number);
		Assert.AreEqual(1, remissionNote.Items.Count);
		Assert.AreSame(stockItem, remissionNote.Items[0].StockItem);
		Assert.AreEqual(2, remissionNote.Items[0].Amount);
		Assert.AreEqual("", remissionNote.Cdc);
		_remissionNotes.Verify(provider => provider.AddAsync(remissionNote, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task CreateAsync_ContingencyModeActive_IssuesCdcLocally()
	{
		// Arrange
		var cdc = "0" + new string('7', 43);
		_contingencyCdcIssuer.Setup(issuer => issuer.TryIssueAsync(SifenDocumentType.NotaDeRemisionElectronica, It.IsAny<CancellationToken>())).ReturnsAsync(cdc);
		var service = this.CreateService();
		var stockItem = new StockItem { Code = "A1", Name = "Screw", Amount = 10 };

		// Act
		var remissionNote = await service.CreateAsync(new Customer(), [(stockItem, 2)], RemissionReason.Venta, "Avda. España 500", DateTime.Today);

		// Assert
		Assert.AreEqual(cdc, remissionNote.Cdc);
	}

	[TestMethod]
	public async Task CreateAsync_NoItems_NeverReservesCdc()
	{
		// Arrange (#167): a rejected remission note must never burn a reserved contingency number
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.CreateAsync(new Customer(), [], RemissionReason.Venta, "addr", DateTime.Today));
		_contingencyCdcIssuer.Verify(issuer => issuer.TryIssueAsync(It.IsAny<SifenDocumentType>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CreateAsync_NonPositiveAmount_NeverReservesCdc()
	{
		// Arrange (#167): a rejected remission note must never burn a reserved contingency number
		var service = this.CreateService();
		var stockItem = new StockItem { Code = "A1", Name = "Screw", Amount = 10 };

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => service.CreateAsync(new Customer(), [(stockItem, 0)], RemissionReason.Venta, "addr", DateTime.Today));
		_contingencyCdcIssuer.Verify(issuer => issuer.TryIssueAsync(It.IsAny<SifenDocumentType>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task CreateAsync_NoItems_Throws()
	{
		// Arrange
		var service = this.CreateService();

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.CreateAsync(new Customer(), [], RemissionReason.Venta, "addr", DateTime.Today));
	}

	[TestMethod]
	public async Task CreateAsync_NonPositiveAmount_Throws()
	{
		// Arrange
		var service = this.CreateService();
		var stockItem = new StockItem { Code = "A1", Name = "Screw", Amount = 10 };

		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => service.CreateAsync(new Customer(), [(stockItem, 0)], RemissionReason.Venta, "addr", DateTime.Today));
	}

	private RemissionNoteService CreateService()
	{
		return new RemissionNoteService(_remissionNotes.Object, _settings.Object, _contingencyCdcIssuer.Object);
	}
}
