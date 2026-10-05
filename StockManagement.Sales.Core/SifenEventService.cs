using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Kernel.Util;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sales.Core;


internal class SifenEventService(
	IInvoiceServiceProvider invoiceServiceProvider,
	ICancellationRequestServiceProvider cancellationRequestServiceProvider,
	IInvoiceNumberVoidServiceProvider invoiceNumberVoidServiceProvider,
	ISettingsService settingsService) : ISifenEventService
{
	private const int MaxVoidRangeSize = 1000;
	private const int MaxReasonLength = 150;

	private readonly IInvoiceServiceProvider _invoiceServiceProvider = invoiceServiceProvider;
	private readonly ICancellationRequestServiceProvider _cancellationRequestServiceProvider = cancellationRequestServiceProvider;
	private readonly IInvoiceNumberVoidServiceProvider _invoiceNumberVoidServiceProvider = invoiceNumberVoidServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;


	public async Task<CancellationRequest> RequestCancellationAsync(string invoiceNumber, string reason, DateTime now, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A cancellation needs a reason.", nameof(reason));

		if (await _invoiceServiceProvider.GetInvoiceAync(invoiceNumber, cancellationToken) is not Invoice invoice)
			throw new InvalidOperationException($"Invoice '{invoiceNumber}' does not exist.");

		if (invoice.TransmissionStatus != TransmissionStatus.Accepted)
			throw new InvalidOperationException($"Invoice '{invoiceNumber}' is not {TransmissionStatus.Accepted} (currently {invoice.TransmissionStatus}); only an accepted DE can be cancelled.");

		if (await _cancellationRequestServiceProvider.GetOpenForInvoiceAsync(invoice, cancellationToken) is not null)
			throw new InvalidOperationException($"Invoice '{invoiceNumber}' already has a cancellation in progress.");

		var deadline = (invoice.AcceptedAt ?? invoice.Date) + SifenEventPolicy.CancellationDeadline;
		if (now >= deadline)
			throw new InvalidOperationException($"Invoice '{invoiceNumber}' is past the 48h Cancelación window (accepted {invoice.AcceptedAt}).");

		var request = new CancellationRequest(invoice, reason, now);
		await _cancellationRequestServiceProvider.AddAsync(request, cancellationToken);
		return request;
	}

	public async Task<InvoiceNumberVoid> RequestNumberVoidAsync(int rangeStart, int rangeEnd, string reason, DateTime now, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A number void needs a reason.", nameof(reason));
		if (reason.Length > MaxReasonLength) throw new ArgumentException($"Reason must be at most {MaxReasonLength} characters.", nameof(reason));
		if (rangeStart <= 0 || rangeEnd < rangeStart) throw new ArgumentException("RangeEnd must be at or after RangeStart, both positive.");
		if (rangeEnd - rangeStart + 1 > MaxVoidRangeSize) throw new ArgumentException($"A number void can cover at most {MaxVoidRangeSize} numbers.");

		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);

		var invoices = await _invoiceServiceProvider.GetInvoicesAsync(cancellationToken);
		var usedSequences = invoices
			.Select(invoice => InvoiceNumber.TryParseSequence(invoice.Number, companySettings.EstablishmentCode, companySettings.PointOfSaleCode))
			.Where(sequence => sequence.HasValue)
			.Select(sequence => sequence!.Value);
		if (usedSequences.Any(sequence => sequence >= rangeStart && sequence <= rangeEnd))
			throw new ArgumentException("The range overlaps an invoice number already in use.");

		var existingVoids = await _invoiceNumberVoidServiceProvider.GetAllAsync(cancellationToken);
		if (existingVoids.Any(existing => existing.TransmissionStatus != TransmissionStatus.Rejected && rangeStart <= existing.RangeEnd && rangeEnd >= existing.RangeStart))
			throw new ArgumentException("The range overlaps a number void already requested.");

		var numberVoid = new InvoiceNumberVoid(rangeStart, rangeEnd, reason, now);
		await _invoiceNumberVoidServiceProvider.AddAsync(numberVoid, cancellationToken);
		return numberVoid;
	}
}
