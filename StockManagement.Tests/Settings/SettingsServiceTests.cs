using Moq;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Settings.Core;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Tests.Settings;


[TestClass]
public sealed class SettingsServiceTests
{
	private readonly Mock<ISettingsServiceProvider> _settings = new();


	[TestMethod]
	public async Task GetLanguageAsync_NothingStored_ReturnsGerman()
	{
		// Arrange
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AppSettings?)null);

		// Act
		var result = await this.CreateService().GetLanguageAsync();

		// Assert
		Assert.AreEqual(AvailableLanguages.German, result);
	}

	[TestMethod]
	public async Task GetLanguageAsync_Stored_ReturnsIt()
	{
		// Arrange
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings { Language = AvailableLanguages.Spanish });

		// Act
		var result = await this.CreateService().GetLanguageAsync();

		// Assert
		Assert.AreEqual(AvailableLanguages.Spanish, result);
	}

	[TestMethod]
	public async Task SetLanguageAsync_NothingStored_Adds()
	{
		// Arrange
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AppSettings?)null);

		// Act
		await this.CreateService().SetLanguageAsync(AvailableLanguages.English);

		// Assert
		_settings.Verify(provider => provider.AddSettingsAsync(It.Is<AppSettings>(settings => settings.Language == AvailableLanguages.English), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task SetLanguageAsync_AlreadyStored_UpdatesExisting()
	{
		// Arrange
		var stored = new AppSettings { Language = AvailableLanguages.German };
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(stored);

		// Act
		await this.CreateService().SetLanguageAsync(AvailableLanguages.English);

		// Assert
		Assert.AreEqual(AvailableLanguages.English, stored.Language);
		_settings.Verify(provider => provider.UpdateSettingsAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
		_settings.Verify(provider => provider.AddSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task GetCompanySettingsAsync_NothingStored_ReturnsDefaults()
	{
		// Arrange
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AppSettings?)null);

		// Act
		var result = await this.CreateService().GetCompanySettingsAsync();

		// Assert
		Assert.AreEqual(new CompanySettings("", "", "", 10m, 30, 1, 1001, 0), result);
	}

	[TestMethod]
	public async Task GetCompanySettingsAsync_Stored_ReturnsIt()
	{
		// Arrange
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings
		{
			CompanyName = "Acme",
			TaxId = "123456",
			Currency = "PYG",
			VatRatePercent = 5m,
			PaymentTermInDays = 14,
			FirstInvoiceNumber = 100,
			FirstCustomerId = 2000
		});

		// Act
		var result = await this.CreateService().GetCompanySettingsAsync();

		// Assert
		Assert.AreEqual(new CompanySettings("Acme", "123456", "PYG", 5m, 14, 100, 2000, 0), result);
	}

	[TestMethod]
	public async Task SetCompanySettingsAsync_NothingStored_Adds()
	{
		// Arrange
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AppSettings?)null);
		var settings = new CompanySettings("Acme", "123456", "PYG", 5m, 14, 100, 2000, 0);

		// Act
		await this.CreateService().SetCompanySettingsAsync(settings);

		// Assert
		_settings.Verify(provider => provider.AddSettingsAsync(It.Is<AppSettings>(stored =>
			stored.CompanyName == "Acme" && stored.TaxId == "123456" && stored.Currency == "PYG" &&
			stored.VatRatePercent == 5m && stored.PaymentTermInDays == 14 &&
			stored.FirstInvoiceNumber == 100 && stored.FirstCustomerId == 2000 && stored.CurrencyDecimalDigits == 0), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task SetCompanySettingsAsync_AlreadyStored_UpdatesExisting()
	{
		// Arrange
		var stored = new AppSettings { Language = AvailableLanguages.Spanish };
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(stored);
		var settings = new CompanySettings("Acme", "123456", "PYG", 5m, 14, 100, 2000, 0);

		// Act
		await this.CreateService().SetCompanySettingsAsync(settings);

		// Assert
		Assert.AreEqual("Acme", stored.CompanyName);
		Assert.AreEqual(5m, stored.VatRatePercent);
		Assert.AreEqual(AvailableLanguages.Spanish, stored.Language);
		_settings.Verify(provider => provider.UpdateSettingsAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
		_settings.Verify(provider => provider.AddSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task GetPrinterSettingsAsync_NothingStored_ReturnsDefaults()
	{
		// Arrange
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AppSettings?)null);

		// Act
		var result = await this.CreateService().GetPrinterSettingsAsync();

		// Assert
		Assert.AreEqual(new PrinterSettings("", 80, KudeFormat.Ticket, false), result);
	}

	[TestMethod]
	public async Task SetPrinterSettingsAsync_AlreadyStored_UpdatesExisting()
	{
		// Arrange
		var stored = new AppSettings { Language = AvailableLanguages.Spanish };
		_settings.Setup(provider => provider.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(stored);
		var settings = new PrinterSettings("Front counter", 58, KudeFormat.Ticket, true);

		// Act
		await this.CreateService().SetPrinterSettingsAsync(settings);

		// Assert
		Assert.AreEqual("Front counter", stored.DefaultPrinterName);
		Assert.AreEqual(58, stored.ReceiptPaperWidthMm);
		Assert.AreEqual(KudeFormat.Ticket, stored.KudeFormat);
		Assert.IsTrue(stored.PrintOnSaleComplete);
		Assert.AreEqual(AvailableLanguages.Spanish, stored.Language);
		_settings.Verify(provider => provider.UpdateSettingsAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
	}

	private SettingsService CreateService()
	{
		return new SettingsService(_settings.Object);
	}
}
