using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Sales.Core;


internal class PaymentService(IInvoiceServiceProvider invoiceServiceProvider, ISettingsService settingsService, ICashRegisterService cashRegisterService) : IPaymentService
{
	private readonly IInvoiceServiceProvider _invoiceServiceProvider = invoiceServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;
	private readonly ICashRegisterService _cashRegisterService = cashRegisterService;


	public decimal GetAmountPaid(Invoice invoice)
	{
		return InvoiceStatusCalculator.AmountPaid(invoice);
	}

	public decimal GetAmountDue(Invoice invoice)
	{
		return InvoiceStatusCalculator.AmountDue(invoice);
	}

	public InvoiceStatus GetStatus(Invoice invoice, DateTime asOf)
	{
		return InvoiceStatusCalculator.GetStatus(invoice, asOf);
	}

	public async Task<RecordPaymentResult> RecordPaymentAsync(string invoiceNumber, decimal amount, PaymentMethod method, DateTime date, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (await _invoiceServiceProvider.GetInvoiceAync(invoiceNumber, cancellationToken) is not Invoice invoice) return RecordPaymentResult.Failure(RecordPaymentError.InvoiceNotFound);

		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		var roundedAmount = Math.Round(amount, companySettings.CurrencyDecimalDigits, MidpointRounding.AwayFromZero);
		if (roundedAmount <= 0) return RecordPaymentResult.Failure(RecordPaymentError.InvalidAmount);
		if (roundedAmount > InvoiceStatusCalculator.AmountDue(invoice)) return RecordPaymentResult.Failure(RecordPaymentError.ExceedsAmountDue);

		var openSession = method == PaymentMethod.Cash ? await _cashRegisterService.GetOpenSessionAsync(cancellationToken) : null;
		var payment = new Payment { Date = date, Amount = roundedAmount, Method = method, CashRegisterSessionId = openSession?.Id };
		invoice.Payments.Add(payment);
		await _invoiceServiceProvider.UpdateInvoiceAsync(invoice, cancellationToken);

		return RecordPaymentResult.Success(payment);
	}

	public async Task<CursorPage<Invoice>> GetOpenInvoicesAsync(int? customerId, string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var now = DateTime.Now;
		var invoices = await this.LoadInvoicesAsync(customerId, cancellationToken);
		var open = invoices.Where(invoice => InvoiceStatusCalculator.GetStatus(invoice, now) is not (InvoiceStatus.Paid or InvoiceStatus.Cancelled));
		return Paginate(open, cursor, pageSize);
	}

	public async Task<CursorPage<Invoice>> GetOverdueInvoicesAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var now = DateTime.Now;
		var invoices = await this.LoadInvoicesAsync(customerId: null, cancellationToken);
		var overdue = invoices.Where(invoice => InvoiceStatusCalculator.GetStatus(invoice, now) == InvoiceStatus.Overdue);
		return Paginate(overdue, cursor, pageSize);
	}

	private async Task<IEnumerable<Invoice>> LoadInvoicesAsync(int? customerId, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var invoices = await _invoiceServiceProvider.GetInvoicesAsync(cancellationToken) ?? [];
		return customerId is int id ? invoices.Where(invoice => invoice.Customer?.CustomerId == id) : invoices;
	}

	private static CursorPage<Invoice> Paginate(IEnumerable<Invoice> invoices, string? cursor, int pageSize)
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
