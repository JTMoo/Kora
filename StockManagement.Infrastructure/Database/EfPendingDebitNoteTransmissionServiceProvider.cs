using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IPendingDebitNoteTransmissionServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class EfPendingDebitNoteTransmissionServiceProvider(AppDbContext db) : IPendingDebitNoteTransmissionServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task<IReadOnlyList<PendingDebitNoteTransmission>> GetDueAsync(DateTime asOf, int maxCount, CancellationToken cancellationToken = default)
	{
		return await _db.PendingDebitNoteTransmissions
			.Where(transmission => transmission.NextAttemptAt <= asOf)
			.OrderBy(transmission => transmission.NextAttemptAt)
			.Take(maxCount)
			.ToListAsync(cancellationToken);
	}

	public async Task MarkTerminalAsync(PendingDebitNoteTransmission transmission, TransmissionStatus status, string cdc, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(transmission);

		await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
		transmission.DebitNote.TransmissionStatus = status;
		if (!string.IsNullOrEmpty(cdc)) transmission.DebitNote.Cdc = cdc;
		_db.PendingDebitNoteTransmissions.Remove(transmission);
		await _db.SaveChangesAsync(cancellationToken);
		await dbTransaction.CommitAsync(cancellationToken);
	}

	public async Task MarkErrorAsync(PendingDebitNoteTransmission transmission, string error, DateTime? nextAttemptAt, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(transmission);

		await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
		transmission.DebitNote.TransmissionStatus = TransmissionStatus.Error;
		transmission.LastError = error;
		transmission.Attempts++;

		if (nextAttemptAt is DateTime next)
		{
			transmission.NextAttemptAt = next;
		}
		else
		{
			_db.PendingDebitNoteTransmissions.Remove(transmission);
		}

		await _db.SaveChangesAsync(cancellationToken);
		await dbTransaction.CommitAsync(cancellationToken);
	}
}
