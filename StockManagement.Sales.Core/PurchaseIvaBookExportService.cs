using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Sales.Core;


/// <remarks>
/// Document-type code "1" is a placeholder (received invoice); Hechauka's actual codes for self-invoices and
/// debit/credit notes received are unverified (#186) - same caveat as <see cref="PurchaseIvaBookRow"/>.
/// <see cref="GoodsImportDocument"/> covers customs imports only, not domestic supplier purchases - there is no
/// domestic purchase-invoice recording feature yet, so this book is import documents only.
/// </remarks>
internal class PurchaseIvaBookExportService(IGoodsImportDocumentServiceProvider goodsImportDocumentServiceProvider, ISettingsService settingsService) : IPurchaseIvaBookExportService
{
	private const string ReceivedInvoiceCode = "1";

	private readonly IGoodsImportDocumentServiceProvider _goodsImportDocumentServiceProvider = goodsImportDocumentServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;


	public async Task<IReadOnlyList<PurchaseIvaBookRow>> GetRowsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
	{
		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		var digits = companySettings.CurrencyDecimalDigits;

		List<PurchaseIvaBookRow> rows = [];
		string? cursor = null;
		do
		{
			cancellationToken.ThrowIfCancellationRequested();
			var page = await _goodsImportDocumentServiceProvider.GetGoodsImportDocumentsAsync(cursor, 100, cancellationToken);
			rows.AddRange(page.Items.Where(document => document.Date >= from && document.Date <= to).Select(document => ToRow(document, digits)));
			cursor = page.NextCursor;
		} while (cursor != null);

		return [.. rows.OrderByDescending(row => row.Date)];
	}

	/// <remarks>
	/// Unlike <see cref="IvaBreakdown"/> (sale <see cref="StockItem.Price"/>), the taxed base here is the purchase
	/// value (<see cref="StockItem.PurchasePrice"/> * <see cref="StockItem.PurchaseExchangeRate"/>, PY0-0020).
	/// </remarks>
	private static PurchaseIvaBookRow ToRow(GoodsImportDocument document, int digits)
	{
		decimal taxed10 = 0, vat10 = 0, taxed5 = 0, vat5 = 0, exempt = 0;
		var lines = (document.Items ?? []).Select(line => ((decimal)line.Amount, line.StockItem.PurchasePrice * line.StockItem.PurchaseExchangeRate, line.StockItem.VatRatePercent));
		foreach (var group in VatRateGroup.ByRate(lines, digits))
		{
			if (group.Rate == 10) { taxed10 += group.TaxedBase; vat10 += group.Vat; }
			else if (group.Rate == 5) { taxed5 += group.TaxedBase; vat5 += group.Vat; }
			else exempt += group.GrossTotal;
		}

		var total = taxed10 + vat10 + taxed5 + vat5 + exempt;
		return new PurchaseIvaBookRow(ReceivedInvoiceCode, document.ProformaNumber, document.Date, "", document.Supplier?.Name ?? "",
			taxed10, vat10, taxed5, vat5, exempt, total);
	}
}
