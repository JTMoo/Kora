using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IPendingTransmissionServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class EfPendingTransmissionServiceProvider(AppDbContext db) : IPendingTransmissionServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task<IReadOnlyList<PendingTransmission>> GetDueAsync(DateTime asOf, int maxCount, CancellationToken cancellationToken = default)
	{
		return await _db.PendingTransmissions
			.Where(transmission => transmission.NextAttemptAt <= asOf)
			.OrderBy(transmission => transmission.NextAttemptAt)
			.Take(maxCount)
			.ToListAsync(cancellationToken);
	}

	public async Task MarkTerminalAsync(PendingTransmission transmission, TransmissionStatus status, string cdc, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(transmission);

		await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
		transmission.Invoice.TransmissionStatus = status;
		if (!string.IsNullOrEmpty(cdc)) transmission.Invoice.Cdc = cdc;
		// Anchors the 48h Cancelación window (#206)
		if (status == TransmissionStatus.Accepted) transmission.Invoice.AcceptedAt = DateTime.Now;
		_db.PendingTransmissions.Remove(transmission);
		await _db.SaveChangesAsync(cancellationToken);
		await dbTransaction.CommitAsync(cancellationToken);
	}

	public async Task MarkErrorAsync(PendingTransmission transmission, string error, DateTime? nextAttemptAt, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(transmission);

		await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
		transmission.Invoice.TransmissionStatus = TransmissionStatus.Error;
		transmission.LastError = error;
		transmission.Attempts++;

		if (nextAttemptAt is DateTime next)
		{
			transmission.NextAttemptAt = next;
		}
		else
		{
			// 72h deadline passed: stop retrying, leaves a stuck Error invoice for the required UI list
			_db.PendingTransmissions.Remove(transmission);
		}

		await _db.SaveChangesAsync(cancellationToken);
		await dbTransaction.CommitAsync(cancellationToken);
	}
}
