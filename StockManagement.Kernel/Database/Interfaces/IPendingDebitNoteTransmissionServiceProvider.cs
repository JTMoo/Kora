using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Database.Interfaces;


/// <summary>
/// Outbox queue for SIFEN transmission of <see cref="DebitNote"/>s (#184) - same contract as
/// <see cref="IPendingRemisionTransmissionServiceProvider"/>, kept separate because it points at a different document entity.
/// </summary>
public interface IPendingDebitNoteTransmissionServiceProvider
{
	public Task<IReadOnlyList<PendingDebitNoteTransmission>> GetDueAsync(DateTime asOf, int maxCount, CancellationToken cancellationToken = default);

	public Task MarkTerminalAsync(PendingDebitNoteTransmission transmission, TransmissionStatus status, string cdc, CancellationToken cancellationToken = default);

	public Task MarkErrorAsync(PendingDebitNoteTransmission transmission, string error, DateTime? nextAttemptAt, CancellationToken cancellationToken = default);
}
