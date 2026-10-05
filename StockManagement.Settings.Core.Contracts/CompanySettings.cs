namespace StockManagement.Settings.Core.Contracts;


/// <summary>
/// Company-wide settings used across sales and imports
/// </summary>
/// <param name="CompanyName">Legal name shown on invoices</param>
/// <param name="TaxId">Tax identification number shown on invoices</param>
/// <param name="Currency">ISO 4217 currency code (e.g. "PYG"); display only, not tied to <paramref name="CurrencyDecimalDigits"/></param>
/// <param name="VatRatePercent">VAT rate as a percentage (e.g. 10 for 10 %), included in stock item prices</param>
/// <param name="PaymentTermInDays">Days from an invoice's date to its due date</param>
/// <param name="FirstInvoiceNumber">Seed for the next invoice number when no invoice exists yet</param>
/// <param name="FirstCustomerId">Seed for the next customer id when no customer exists yet</param>
/// <param name="CurrencyDecimalDigits">Decimal digits invoice totals/tax round to (0 for a zero-decimal currency like PYG)</param>
/// <param name="Ruc">SIFEN taxpayer RUC (with check digit, e.g. "1234567-8")</param>
/// <param name="TimbradoNumber">DNIT-issued timbrado number</param>
/// <param name="TimbradoValidFrom">First day the timbrado may be used</param>
/// <param name="TimbradoValidTo">Last day the timbrado may be used</param>
/// <param name="EstablishmentCode">DNIT establishment code (3 digits), part of the composite invoice number</param>
/// <param name="PointOfSaleCode">DNIT point-of-sale code (3 digits), part of the composite invoice number</param>
/// <param name="EstablishmentAddress">Street address of the establishment issuing the DTE (SIFEN <c>dDirEmi</c>)</param>
public sealed record CompanySettings(string CompanyName, string TaxId, string Currency, decimal VatRatePercent, int PaymentTermInDays, int FirstInvoiceNumber, int FirstCustomerId, int CurrencyDecimalDigits,
	string Ruc = "", string TimbradoNumber = "", DateTime? TimbradoValidFrom = null, DateTime? TimbradoValidTo = null, string EstablishmentCode = "001", string PointOfSaleCode = "001", string EstablishmentAddress = "");
