using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IDebitNoteServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class DebitNoteServiceProvider(AppDbContext db) : IDebitNoteServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task<IReadOnlyList<DebitNote>> GetDebitNotesAsync(CancellationToken cancellationToken = default)
	{
		return await _db.DebitNotes.OrderByDescending(debitNote => debitNote.Date).ToListAsync(cancellationToken);
	}

	public Task<DebitNote?> GetDebitNoteAsync(string number, CancellationToken cancellationToken = default)
	{
		return _db.DebitNotes.SingleOrDefaultAsync(debitNote => debitNote.Number == number, cancellationToken);
	}

	/// <exception cref="DebitNoteNumberAlreadyExistsException">Number already exists; nothing written</exception>
	public async Task AddAsync(DebitNote debitNote, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(debitNote);

		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		// Reuse the tracked instance, same reason as InvoiceServiceProvider.TryAddSaleAsync
		debitNote.Invoice = await _db.Invoices.FindAsync([debitNote.Invoice.Id], cancellationToken) ?? debitNote.Invoice;
		_db.DebitNotes.Add(debitNote);
		_db.PendingDebitNoteTransmissions.Add(new PendingDebitNoteTransmission(debitNote, DateTime.Now));

		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			await transaction.RollbackAsync(cancellationToken);
			throw new DebitNoteNumberAlreadyExistsException();
		}

		await transaction.CommitAsync(cancellationToken);
	}
}
