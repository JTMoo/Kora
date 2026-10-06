using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Settings.Core;


internal class SettingsService(ISettingsServiceProvider settingsServiceProvider) : ISettingsService
{
	private readonly ISettingsServiceProvider _settingsServiceProvider = settingsServiceProvider;


	public async Task<AvailableLanguages> GetLanguageAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var settings = await _settingsServiceProvider.GetSettingsAsync(cancellationToken);
		return settings?.Language ?? AvailableLanguages.German;
	}

	public async Task SetLanguageAsync(AvailableLanguages language, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (await _settingsServiceProvider.GetSettingsAsync(cancellationToken) is AppSettings settings)
		{
			settings.Language = language;
			await _settingsServiceProvider.UpdateSettingsAsync(settings, cancellationToken);
		}
		else
		{
			await _settingsServiceProvider.AddSettingsAsync(new AppSettings { Language = language }, cancellationToken);
		}
	}

	public async Task<CompanySettings> GetCompanySettingsAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var settings = await _settingsServiceProvider.GetSettingsAsync(cancellationToken) ?? new AppSettings();
		return new CompanySettings(settings.CompanyName, settings.TaxId, settings.Currency, settings.VatRatePercent, settings.PaymentTermInDays, settings.FirstInvoiceNumber, settings.FirstCustomerId, settings.CurrencyDecimalDigits,
			settings.Ruc, settings.TimbradoNumber, settings.TimbradoValidFrom, settings.TimbradoValidTo, settings.EstablishmentCode, settings.PointOfSaleCode, settings.EstablishmentAddress);
	}

	public async Task SetCompanySettingsAsync(CompanySettings companySettings, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ArgumentNullException.ThrowIfNull(companySettings);

		if (await _settingsServiceProvider.GetSettingsAsync(cancellationToken) is AppSettings settings)
		{
			settings.CompanyName = companySettings.CompanyName;
			settings.TaxId = companySettings.TaxId;
			settings.Currency = companySettings.Currency;
			settings.VatRatePercent = companySettings.VatRatePercent;
			settings.PaymentTermInDays = companySettings.PaymentTermInDays;
			settings.FirstInvoiceNumber = companySettings.FirstInvoiceNumber;
			settings.FirstCustomerId = companySettings.FirstCustomerId;
			settings.CurrencyDecimalDigits = companySettings.CurrencyDecimalDigits;
			settings.Ruc = companySettings.Ruc;
			settings.TimbradoNumber = companySettings.TimbradoNumber;
			settings.TimbradoValidFrom = companySettings.TimbradoValidFrom;
			settings.TimbradoValidTo = companySettings.TimbradoValidTo;
			settings.EstablishmentCode = companySettings.EstablishmentCode;
			settings.PointOfSaleCode = companySettings.PointOfSaleCode;
			settings.EstablishmentAddress = companySettings.EstablishmentAddress;
			await _settingsServiceProvider.UpdateSettingsAsync(settings, cancellationToken);
		}
		else
		{
			await _settingsServiceProvider.AddSettingsAsync(new AppSettings
			{
				CompanyName = companySettings.CompanyName,
				TaxId = companySettings.TaxId,
				Currency = companySettings.Currency,
				VatRatePercent = companySettings.VatRatePercent,
				PaymentTermInDays = companySettings.PaymentTermInDays,
				FirstInvoiceNumber = companySettings.FirstInvoiceNumber,
				FirstCustomerId = companySettings.FirstCustomerId,
				CurrencyDecimalDigits = companySettings.CurrencyDecimalDigits,
				Ruc = companySettings.Ruc,
				TimbradoNumber = companySettings.TimbradoNumber,
				TimbradoValidFrom = companySettings.TimbradoValidFrom,
				TimbradoValidTo = companySettings.TimbradoValidTo,
				EstablishmentCode = companySettings.EstablishmentCode,
				PointOfSaleCode = companySettings.PointOfSaleCode,
				EstablishmentAddress = companySettings.EstablishmentAddress
			}, cancellationToken);
		}
	}

	public async Task<PrinterSettings> GetPrinterSettingsAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var settings = await _settingsServiceProvider.GetSettingsAsync(cancellationToken) ?? new AppSettings();
		return new PrinterSettings(settings.DefaultPrinterName, settings.ReceiptPaperWidthMm, settings.KudeFormat, settings.PrintOnSaleComplete);
	}

	public async Task SetPrinterSettingsAsync(PrinterSettings printerSettings, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ArgumentNullException.ThrowIfNull(printerSettings);

		if (await _settingsServiceProvider.GetSettingsAsync(cancellationToken) is AppSettings settings)
		{
			settings.DefaultPrinterName = printerSettings.DefaultPrinterName;
			settings.ReceiptPaperWidthMm = printerSettings.ReceiptPaperWidthMm;
			settings.KudeFormat = printerSettings.KudeFormat;
			settings.PrintOnSaleComplete = printerSettings.PrintOnSaleComplete;
			await _settingsServiceProvider.UpdateSettingsAsync(settings, cancellationToken);
		}
		else
		{
			await _settingsServiceProvider.AddSettingsAsync(new AppSettings
			{
				DefaultPrinterName = printerSettings.DefaultPrinterName,
				ReceiptPaperWidthMm = printerSettings.ReceiptPaperWidthMm,
				KudeFormat = printerSettings.KudeFormat,
				PrintOnSaleComplete = printerSettings.PrintOnSaleComplete
			}, cancellationToken);
		}
	}
}
