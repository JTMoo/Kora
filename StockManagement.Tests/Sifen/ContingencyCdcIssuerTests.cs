using Moq;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class ContingencyCdcIssuerTests
{
	private readonly Mock<IContingencyCdcRangeServiceProvider> _rangeServiceProvider = new();
	private readonly Mock<ISettingsService> _settingsService = new();

	[TestInitialize]
	public void Initialize()
	{
		_settingsService.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0, Ruc: "1946520-3", EstablishmentCode: "001", PointOfSaleCode: "002"));
	}

	[TestMethod]
	public async Task TryIssueAsync_RangeExhaustedOrInactive_ReturnsNull()
	{
		// Arrange
		_rangeServiceProvider.Setup(provider => provider.TryReserveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync((long?)null);

		// Act
		var cdc = await this.CreateIssuer().TryIssueAsync(SifenDocumentType.FacturaElectronica);

		// Assert
		Assert.IsNull(cdc);
	}

	[TestMethod]
	public async Task TryIssueAsync_RangeActive_ReturnsContingencyCdcForReservedNumber()
	{
		// Arrange
		_rangeServiceProvider.Setup(provider => provider.TryReserveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42L);

		// Act
		var cdc = await this.CreateIssuer().TryIssueAsync(SifenDocumentType.FacturaElectronica);

		// Assert
		Assert.IsNotNull(cdc);
		Assert.AreEqual(44, cdc!.Length);
		Assert.AreEqual("01", cdc[0..2]); // iTiDE = FacturaElectronica
		Assert.AreEqual("0000042", cdc[17..24]); // document number = the reserved number
		Assert.AreEqual("2", cdc[33..34]); // iTipEmi = Contingencia
	}

	[TestMethod]
	public async Task TryIssueAsync_RemisionDocumentType_UsesRemisionDocTypeInCdc()
	{
		// Arrange (#188): same reserved-number sequence, different SIFEN document type
		_rangeServiceProvider.Setup(provider => provider.TryReserveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7L);

		// Act
		var cdc = await this.CreateIssuer().TryIssueAsync(SifenDocumentType.NotaDeRemisionElectronica);

		// Assert
		Assert.IsNotNull(cdc);
		Assert.AreEqual("07", cdc![0..2]); // iTiDE = NotaDeRemisionElectronica
		Assert.AreEqual("0000007", cdc[17..24]);
		Assert.AreEqual("2", cdc[33..34]); // iTipEmi = Contingencia
	}

	[TestMethod]
	public async Task TryIssueAsync_InvalidCompanyRuc_Throws()
	{
		// Arrange
		_settingsService.Setup(service => service.GetCompanySettingsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0, Ruc: ""));
		_rangeServiceProvider.Setup(provider => provider.TryReserveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1L);

		// Act + Assert
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => this.CreateIssuer().TryIssueAsync(SifenDocumentType.FacturaElectronica));
	}

	[TestMethod]
	public async Task ConfigureRangeAsync_RangeEndNotAfterStart_Throws()
	{
		// Act + Assert
		await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => this.CreateIssuer().ConfigureRangeAsync(100, 100));
	}

	[TestMethod]
	public async Task ConfigureRangeAsync_ValidRange_DelegatesToProvider()
	{
		// Act
		await this.CreateIssuer().ConfigureRangeAsync(1, 1000);

		// Assert
		_rangeServiceProvider.Verify(provider => provider.SetRangeAsync(1, 1000, It.IsAny<CancellationToken>()), Times.Once);
	}

	private ContingencyCdcIssuer CreateIssuer()
	{
		return new ContingencyCdcIssuer(_rangeServiceProvider.Object, new CdcGenerator(), _settingsService.Object);
	}
}
