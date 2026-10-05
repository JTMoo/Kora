using StockManagement.Kernel.Model;
using StockManagement.Kernel.Util;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <summary>
/// Maps an already-transmitted/contingency-issued <see cref="Invoice"/>/<see cref="RemissionNote"/> to the data
/// <see cref="IKudeHtmlBuilder"/> needs (#183, ADR-0035).
/// </summary>
/// <remarks>
/// Deliberately separate from <see cref="DirectDnitSifenGateway"/>'s own entity mapping, which mints a new CDC
/// when one is missing (correct for transmission). A KuDE must never mint a CDC of its own - it would disagree
/// with whatever was actually (or will be) transmitted - so this throws instead.
/// </remarks>
public static class KudeDataMapper
{
	/// <exception cref="InvalidOperationException"><paramref name="invoice"/> has no CDC yet, or company settings are incomplete.</exception>
	public static DteInvoiceData ToInvoiceData(Invoice invoice, CompanySettings companySettings)
	{
		ArgumentNullException.ThrowIfNull(invoice);
		ArgumentNullException.ThrowIfNull(companySettings);
		if (string.IsNullOrEmpty(invoice.Cdc))
			throw new InvalidOperationException("Invoice has no CDC yet; it has not been transmitted or contingency-issued.");

		var emisor = ToEmisor(companySettings);
		var receptor = ToReceptor(invoice.Customer);
		var documentNumber = long.Parse(invoice.Cdc.AsSpan(17, 7));

		var items = invoice.Items.Select(item => new DteItem(
			item.StockItem.Code,
			item.StockItem.Name,
			item.Amount,
			item.StockItem.Price,
			item.StockItem.VatRatePercent)).ToList();

		return new DteInvoiceData(invoice.Cdc, emisor, receptor, invoice.Date, documentNumber, items, companySettings.CurrencyDecimalDigits);
	}

	/// <exception cref="InvalidOperationException"><paramref name="remissionNote"/> has no CDC yet, or company settings are incomplete.</exception>
	public static DteRemisionData ToRemisionData(RemissionNote remissionNote, CompanySettings companySettings)
	{
		ArgumentNullException.ThrowIfNull(remissionNote);
		ArgumentNullException.ThrowIfNull(companySettings);
		if (string.IsNullOrEmpty(remissionNote.Cdc))
			throw new InvalidOperationException("Remission note has no CDC yet; it has not been transmitted or contingency-issued.");

		var emisor = ToEmisor(companySettings);
		var receptor = ToReceptor(remissionNote.Customer);
		var documentNumber = long.Parse(remissionNote.Cdc.AsSpan(17, 7));

		var items = remissionNote.Items.Select(item => new DteRemisionItem(
			item.StockItem.Code,
			item.StockItem.Name,
			item.Amount)).ToList();

		return new DteRemisionData(remissionNote.Cdc, emisor, receptor, remissionNote.Date, documentNumber, remissionNote.Reason, remissionNote.DestinationAddress, items);
	}

	/// <exception cref="InvalidOperationException">Company settings are missing data a DE needs.</exception>
	private static DteEmisor ToEmisor(CompanySettings companySettings)
	{
		if (!RucValidator.TryNormalize(companySettings.Ruc, out var normalizedRuc))
			throw new InvalidOperationException("Company settings RUC is missing or invalid; set it before generating a KuDE.");
		if (companySettings.TimbradoValidFrom is not DateTime timbradoValidFrom)
			throw new InvalidOperationException("Company settings timbrado validity start is missing; set it before generating a KuDE.");

		var rucParts = normalizedRuc.Split('-');
		return new DteEmisor(
			rucParts[0],
			int.Parse(rucParts[1]),
			companySettings.CompanyName,
			companySettings.EstablishmentCode,
			companySettings.PointOfSaleCode,
			companySettings.EstablishmentAddress,
			companySettings.TimbradoNumber,
			DateOnly.FromDateTime(timbradoValidFrom));
	}

	private static DteReceptor ToReceptor(Customer customer)
	{
		return new DteReceptor(customer.Display, RucBase: null, RucCheckDigit: null, customer.IdentificationNumber);
	}
}
