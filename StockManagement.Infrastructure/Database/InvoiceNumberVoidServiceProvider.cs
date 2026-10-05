using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IInvoiceNumberVoidServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class InvoiceNumberVoidServiceProvider(AppDbContext db) : IInvoiceNumberVoidServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task AddAsync(InvoiceNumberVoid numberVoid, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(numberVoid);

		await _db.InvoiceNumberVoids.AddAsync(numberVoid, cancellationToken);
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<InvoiceNumberVoid>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _db.InvoiceNumberVoids.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<InvoiceNumberVoid>> GetDueAsync(DateTime asOf, int maxCount, CancellationToken cancellationToken = default)
	{
		return await _db.InvoiceNumberVoids
			.Where(numberVoid => numberVoid.TransmissionStatus == TransmissionStatus.Pending || numberVoid.TransmissionStatus == TransmissionStatus.Error)
			.Where(numberVoid => numberVoid.NextAttemptAt <= asOf)
			.OrderBy(numberVoid => numberVoid.NextAttemptAt)
			.Take(maxCount)
			.ToListAsync(cancellationToken);
	}

	public async Task MarkTerminalAsync(InvoiceNumberVoid numberVoid, TransmissionStatus status, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(numberVoid);

		numberVoid.TransmissionStatus = status;
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task MarkErrorAsync(InvoiceNumberVoid numberVoid, string error, DateTime? nextAttemptAt, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(numberVoid);

		numberVoid.TransmissionStatus = TransmissionStatus.Error;
		numberVoid.LastError = error;
		numberVoid.Attempts++;

		if (nextAttemptAt is DateTime next)
		{
			numberVoid.NextAttemptAt = next;
		}

		await _db.SaveChangesAsync(cancellationToken);
	}
}
