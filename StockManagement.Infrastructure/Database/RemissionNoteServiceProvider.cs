using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IRemissionNoteServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class RemissionNoteServiceProvider(AppDbContext db) : IRemissionNoteServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task<IReadOnlyList<RemissionNote>> GetRemissionNotesAsync(CancellationToken cancellationToken = default)
	{
		return await _db.RemissionNotes.OrderByDescending(remissionNote => remissionNote.Date).ToListAsync(cancellationToken);
	}

	public Task<RemissionNote?> GetRemissionNoteAsync(string number, CancellationToken cancellationToken = default)
	{
		return _db.RemissionNotes.SingleOrDefaultAsync(remissionNote => remissionNote.Number == number, cancellationToken);
	}

	/// <exception cref="RemissionNoteNumberAlreadyExistsException">Number already exists; nothing written</exception>
	public async Task AddAsync(RemissionNote remissionNote, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(remissionNote);

		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		// Reuse the tracked instance, same reason as InvoiceServiceProvider.TryAddSaleAsync
		remissionNote.Customer = await _db.Customers.FindAsync([remissionNote.Customer.Id], cancellationToken) ?? remissionNote.Customer;
		_db.RemissionNotes.Add(remissionNote);
		_db.PendingRemisionTransmissions.Add(new PendingRemisionTransmission(remissionNote, DateTime.Now));

		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			await transaction.RollbackAsync(cancellationToken);
			throw new RemissionNoteNumberAlreadyExistsException();
		}

		await transaction.CommitAsync(cancellationToken);
	}
}
