using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Database.Interfaces;


/// <summary>
/// Outbox + permanent record for SIFEN Cancelación events (#206). Unlike <see cref="IPendingTransmissionServiceProvider"/>,
/// rows are never removed - they stay as the retained record of the event.
/// </summary>
public interface ICancellationRequestServiceProvider
{
	public Task AddAsync(CancellationRequest request, CancellationToken cancellationToken = default);

	/// <summary>Every cancellation ever requested, for listing</summary>
	public Task<IReadOnlyList<CancellationRequest>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>Open (non-terminal) cancellation request for <paramref name="invoice"/>, if any</summary>
	public Task<CancellationRequest?> GetOpenForInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);

	public Task<IReadOnlyList<CancellationRequest>> GetDueAsync(DateTime asOf, int maxCount, CancellationToken cancellationToken = default);

	/// <summary>
	/// SIFEN gave a final answer: <see cref="TransmissionStatus.Accepted"/> also moves the invoice itself to
	/// <see cref="TransmissionStatus.Cancelled"/>; <see cref="TransmissionStatus.Rejected"/> leaves the invoice as is.
	/// </summary>
	public Task MarkTerminalAsync(CancellationRequest request, TransmissionStatus status, CancellationToken cancellationToken = default);

	public Task MarkErrorAsync(CancellationRequest request, string error, DateTime? nextAttemptAt, CancellationToken cancellationToken = default);
}
