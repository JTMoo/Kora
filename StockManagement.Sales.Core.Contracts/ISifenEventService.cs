using StockManagement.Kernel.Model;

namespace StockManagement.Sales.Core.Contracts;


/// <summary>
/// SIFEN Cancelación and Inutilización events (#206) - act on already-assigned numbers, distinct from the DTE
/// documents <see cref="ISaleService"/>/<see cref="IDebitNoteService"/>/<see cref="IRemissionNoteService"/> emit.
/// </summary>
public interface ISifenEventService
{
	/// <summary>
	/// Requests a Cancelación of <paramref name="invoiceNumber"/>'s DE, queuing it for SIFEN transmission.
	/// </summary>
	/// <exception cref="ArgumentException"><paramref name="reason"/> is empty</exception>
	/// <exception cref="InvalidOperationException">
	/// The invoice doesn't exist, isn't <see cref="Kernel.Model.Types.TransmissionStatus.Accepted"/>, already has an
	/// open or accepted cancellation, or is past the 48h window from <see cref="Invoice.AcceptedAt"/>
	/// </exception>
	public Task<CancellationRequest> RequestCancellationAsync(string invoiceNumber, string reason, DateTime now, CancellationToken cancellationToken = default);

	/// <summary>
	/// Requests an Inutilización of the invoice-number sequence range [<paramref name="rangeStart"/>, <paramref name="rangeEnd"/>]
	/// (inclusive), queuing it for SIFEN transmission.
	/// </summary>
	/// <exception cref="ArgumentException">
	/// <paramref name="reason"/> is empty or over 150 chars, the range is empty/inverted, spans more than 1000 numbers,
	/// overlaps an invoice number already in use, or overlaps a range already voided
	/// </exception>
	public Task<InvoiceNumberVoid> RequestNumberVoidAsync(int rangeStart, int rangeEnd, string reason, DateTime now, CancellationToken cancellationToken = default);
}
