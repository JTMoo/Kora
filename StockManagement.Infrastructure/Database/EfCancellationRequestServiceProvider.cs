using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ICancellationRequestServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class EfCancellationRequestServiceProvider(AppDbContext db) : ICancellationRequestServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task AddAsync(CancellationRequest request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		await _db.CancellationRequests.AddAsync(request, cancellationToken);
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<CancellationRequest>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _db.CancellationRequests.ToListAsync(cancellationToken);
	}

	public async Task<CancellationRequest?> GetOpenForInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(invoice);

		return await _db.CancellationRequests
			.Where(request => request.Invoice.Id == invoice.Id)
			.Where(request => request.TransmissionStatus == TransmissionStatus.Pending || request.TransmissionStatus == TransmissionStatus.Sent || request.TransmissionStatus == TransmissionStatus.Error)
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<CancellationRequest>> GetDueAsync(DateTime asOf, int maxCount, CancellationToken cancellationToken = default)
	{
		return await _db.CancellationRequests
			.Where(request => request.TransmissionStatus == TransmissionStatus.Pending || request.TransmissionStatus == TransmissionStatus.Error)
			.Where(request => request.NextAttemptAt <= asOf)
			.OrderBy(request => request.NextAttemptAt)
			.Take(maxCount)
			.ToListAsync(cancellationToken);
	}

	public async Task MarkTerminalAsync(CancellationRequest request, TransmissionStatus status, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
		request.TransmissionStatus = status;
		if (status == TransmissionStatus.Accepted) request.Invoice.TransmissionStatus = TransmissionStatus.Cancelled;
		await _db.SaveChangesAsync(cancellationToken);
		await dbTransaction.CommitAsync(cancellationToken);
	}

	public async Task MarkErrorAsync(CancellationRequest request, string error, DateTime? nextAttemptAt, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		request.TransmissionStatus = TransmissionStatus.Error;
		request.LastError = error;
		request.Attempts++;

		if (nextAttemptAt is DateTime next)
		{
			request.NextAttemptAt = next;
		}
		// else: the 48h window passed - row stays Error, as the stuck record (same as a stuck invoice transmission)

		await _db.SaveChangesAsync(cancellationToken);
	}
}
