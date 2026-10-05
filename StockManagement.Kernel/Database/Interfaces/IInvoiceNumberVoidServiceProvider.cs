using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Database.Interfaces;


/// <summary>
/// Outbox + permanent record for SIFEN Inutilización events (#206). Same never-removed reasoning as
/// <see cref="ICancellationRequestServiceProvider"/>.
/// </summary>
public interface IInvoiceNumberVoidServiceProvider
{
	public Task AddAsync(InvoiceNumberVoid numberVoid, CancellationToken cancellationToken = default);

	/// <summary>Every void ever requested, for range-overlap checks and listing</summary>
	public Task<IReadOnlyList<InvoiceNumberVoid>> GetAllAsync(CancellationToken cancellationToken = default);

	public Task<IReadOnlyList<InvoiceNumberVoid>> GetDueAsync(DateTime asOf, int maxCount, CancellationToken cancellationToken = default);

	public Task MarkTerminalAsync(InvoiceNumberVoid numberVoid, TransmissionStatus status, CancellationToken cancellationToken = default);

	public Task MarkErrorAsync(InvoiceNumberVoid numberVoid, string error, DateTime? nextAttemptAt, CancellationToken cancellationToken = default);
}
