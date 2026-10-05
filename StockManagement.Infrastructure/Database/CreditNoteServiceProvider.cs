using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ICreditNoteServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class CreditNoteServiceProvider(AppDbContext db) : ICreditNoteServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<CreditNote> GetCreditNoteAsync(int number, CancellationToken cancellationToken = default)
	{
		return _db.CreditNotes.SingleOrDefaultAsync(creditNote => creditNote.Number == number, cancellationToken)!;
	}

	public async Task<IEnumerable<CreditNote>> GetCreditNotesAsync(CancellationToken cancellationToken = default)
	{
		return await _db.CreditNotes.ToListAsync(cancellationToken);
	}

	/// <remarks>
	/// The invoice is claimed with a conditional <c>UPDATE ... WHERE NOT "IsCancelled"</c> first, so a concurrent
	/// cancellation of the same invoice can't restock it twice; stock is then returned per line and the credit
	/// note stored, all in one Postgres transaction.
	/// </remarks>
	/// <exception cref="CreditNoteNumberAlreadyExistsException">Number already exists; nothing written</exception>
	public async Task<bool> TryAddCreditNoteAsync(CreditNote creditNote, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(creditNote);
		var invoice = creditNote.Invoice ?? throw new ArgumentException("CreditNote.Invoice is required.", nameof(creditNote));

		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		var claimed = await _db.Invoices
			.Where(stored => stored.Id == invoice.Id && !stored.IsCancelled)
			.ExecuteUpdateAsync(setters => setters.SetProperty(stored => stored.IsCancelled, true), cancellationToken);

		if (claimed == 0)
		{
			await transaction.RollbackAsync(cancellationToken);
			return false;
		}

		// AutoInclude on Invoice.Items/StockItem (InvoiceConfiguration) loads what TryAddCreditNoteAsync needs to restock
		var storedInvoice = await _db.Invoices.SingleAsync(stored => stored.Id == invoice.Id, cancellationToken);
		foreach (var item in storedInvoice.Items ?? [])
		{
			await _db.StockItems
				.Where(stockItem => stockItem.Id == item.StockItem.Id)
				.ExecuteUpdateAsync(setters => setters.SetProperty(stockItem => stockItem.Amount, stockItem => stockItem.Amount + item.Amount), cancellationToken);

			var stockItem = await _db.StockItems.SingleAsync(stored => stored.Id == item.StockItem.Id, cancellationToken);
			_db.Transactions.Add(new Transaction(stockItem, DateTime.Now, Transaction.Kind.Amount, item.Amount, creditNote.Reason));
		}

		creditNote.Invoice = storedInvoice;
		_db.CreditNotes.Add(creditNote);

		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			await transaction.RollbackAsync(cancellationToken);
			throw new CreditNoteNumberAlreadyExistsException();
		}

		await transaction.CommitAsync(cancellationToken);
		return true;
	}
}
