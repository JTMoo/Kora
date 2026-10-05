using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Api.Features.Settings;


public sealed record CompanySettingsResponse(string CompanyName, string TaxId, string Currency, decimal VatRatePercent, int PaymentTermInDays, int FirstInvoiceNumber, int FirstCustomerId, int CurrencyDecimalDigits,
	string Ruc, string TimbradoNumber, DateTime? TimbradoValidFrom, DateTime? TimbradoValidTo, string EstablishmentCode, string PointOfSaleCode, string EstablishmentAddress)
{
	public static CompanySettingsResponse From(CompanySettings settings)
	{
		return new CompanySettingsResponse(settings.CompanyName, settings.TaxId, settings.Currency, settings.VatRatePercent, settings.PaymentTermInDays, settings.FirstInvoiceNumber, settings.FirstCustomerId, settings.CurrencyDecimalDigits,
			settings.Ruc, settings.TimbradoNumber, settings.TimbradoValidFrom, settings.TimbradoValidTo, settings.EstablishmentCode, settings.PointOfSaleCode, settings.EstablishmentAddress);
	}
}
