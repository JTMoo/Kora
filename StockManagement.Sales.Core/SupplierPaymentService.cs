using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Sales.Core;


internal class SupplierPaymentService(ISupplierInvoiceServiceProvider supplierInvoiceServiceProvider, ISettingsService settingsService) : ISupplierPaymentService
{
	private readonly ISupplierInvoiceServiceProvider _supplierInvoiceServiceProvider = supplierInvoiceServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;


	public decimal GetAmountPaid(SupplierInvoice invoice)
	{
		return SupplierInvoiceStatusCalculator.AmountPaid(invoice);
	}

	public decimal GetAmountDue(SupplierInvoice invoice)
	{
		return SupplierInvoiceStatusCalculator.AmountDue(invoice);
	}

	public SupplierInvoiceStatus GetStatus(SupplierInvoice invoice, DateTime asOf)
	{
		return SupplierInvoiceStatusCalculator.GetStatus(invoice, asOf);
	}

	public async Task<RecordSupplierPaymentResult> RecordPaymentAsync(string invoiceNumber, decimal amount, PaymentMethod method, DateTime date, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (await _supplierInvoiceServiceProvider.GetSupplierInvoiceAsync(invoiceNumber, cancellationToken) is not SupplierInvoice invoice) return RecordSupplierPaymentResult.Failure(RecordSupplierPaymentError.SupplierInvoiceNotFound);

		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		var roundedAmount = Math.Round(amount, companySettings.CurrencyDecimalDigits, MidpointRounding.AwayFromZero);
		if (roundedAmount <= 0) return RecordSupplierPaymentResult.Failure(RecordSupplierPaymentError.InvalidAmount);
		if (roundedAmount > SupplierInvoiceStatusCalculator.AmountDue(invoice)) return RecordSupplierPaymentResult.Failure(RecordSupplierPaymentError.ExceedsAmountDue);

		var payment = new Payment { Date = date, Amount = roundedAmount, Method = method };
		invoice.Payments.Add(payment);
		await _supplierInvoiceServiceProvider.UpdateSupplierInvoiceAsync(invoice, cancellationToken);

		return RecordSupplierPaymentResult.Success(payment);
	}

	public async Task<CursorPage<SupplierInvoice>> GetOpenSupplierInvoicesAsync(string? supplierId, string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var now = DateTime.Now;
		var invoices = await _supplierInvoiceServiceProvider.GetSupplierInvoicesAsync(cancellationToken) ?? [];
		if (supplierId is string id) invoices = invoices.Where(invoice => invoice.Supplier?.Id == id);

		var open = invoices.Where(invoice => SupplierInvoiceStatusCalculator.GetStatus(invoice, now) is not SupplierInvoiceStatus.Paid);
		return Paginate(open, cursor, pageSize);
	}

	/// <summary>Keyset page by <see cref="SupplierInvoice.ExpirationDate"/> then <see cref="Database.BaseDocument.Id"/> (ADR-0029), same shape as <see cref="PaymentService"/></summary>
	private static CursorPage<SupplierInvoice> Paginate(IEnumerable<SupplierInvoice> invoices, string? cursor, int pageSize)
	{
		var ordered = invoices.OrderBy(invoice => invoice.ExpirationDate).ThenBy(invoice => invoice.Id).ToList();

		var startIndex = 0;
		if (Cursor.TryDecode(cursor, 2) is [var dateText, var lastId])
		{
			var lastDate = DateTime.Parse(dateText, null, System.Globalization.DateTimeStyles.RoundtripKind);
			startIndex = ordered.FindIndex(invoice => invoice.ExpirationDate > lastDate || (invoice.ExpirationDate == lastDate && string.CompareOrdinal(invoice.Id, lastId) > 0));
			if (startIndex < 0) startIndex = ordered.Count;
		}

		var items = ordered.Skip(startIndex).Take(pageSize).ToList();
		var nextCursor = startIndex + items.Count < ordered.Count ? Cursor.Encode(items[^1].ExpirationDate.ToString("O"), items[^1].Id) : null;
		return new(items, nextCursor);
	}
}
