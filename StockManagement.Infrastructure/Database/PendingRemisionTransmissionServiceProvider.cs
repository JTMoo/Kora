using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IPendingRemisionTransmissionServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class PendingRemisionTransmissionServiceProvider(AppDbContext db) : IPendingRemisionTransmissionServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task<IReadOnlyList<PendingRemisionTransmission>> GetDueAsync(DateTime asOf, int maxCount, CancellationToken cancellationToken = default)
	{
		return await _db.PendingRemisionTransmissions
			.Where(transmission => transmission.NextAttemptAt <= asOf)
			.OrderBy(transmission => transmission.NextAttemptAt)
			.Take(maxCount)
			.ToListAsync(cancellationToken);
	}

	public async Task MarkTerminalAsync(PendingRemisionTransmission transmission, TransmissionStatus status, string cdc, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(transmission);

		await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
		transmission.RemissionNote.TransmissionStatus = status;
		if (!string.IsNullOrEmpty(cdc)) transmission.RemissionNote.Cdc = cdc;
		_db.PendingRemisionTransmissions.Remove(transmission);
		await _db.SaveChangesAsync(cancellationToken);
		await dbTransaction.CommitAsync(cancellationToken);
	}

	public async Task MarkErrorAsync(PendingRemisionTransmission transmission, string error, DateTime? nextAttemptAt, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(transmission);

		await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
		transmission.RemissionNote.TransmissionStatus = TransmissionStatus.Error;
		transmission.LastError = error;
		transmission.Attempts++;

		if (nextAttemptAt is DateTime next)
		{
			transmission.NextAttemptAt = next;
		}
		else
		{
			_db.PendingRemisionTransmissions.Remove(transmission);
		}

		await _db.SaveChangesAsync(cancellationToken);
		await dbTransaction.CommitAsync(cancellationToken);
	}
}
