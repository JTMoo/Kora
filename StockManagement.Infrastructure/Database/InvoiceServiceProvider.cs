using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IInvoiceServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class InvoiceServiceProvider(AppDbContext db) : IInvoiceServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<Invoice> GetInvoiceAync(string invoiceNumber, CancellationToken cancellationToken = default)
	{
		return _db.Invoices.SingleOrDefaultAsync(invoice => invoice.Number == invoiceNumber, cancellationToken)!;
	}

	public async Task<IEnumerable<Invoice>> GetInvoicesAsync(CancellationToken cancellationToken = default)
	{
		return await _db.Invoices.ToListAsync(cancellationToken);
	}

	public async Task<CursorPage<Invoice>> GetInvoicesAsync(int? customerId, DateTime? from, DateTime? to, string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = _db.Invoices.AsQueryable();
		if (customerId is int id) query = query.Where(invoice => invoice.Customer.CustomerId == id);
		if (from is DateTime start) query = query.Where(invoice => invoice.Date >= start);
		if (to is DateTime end) query = query.Where(invoice => invoice.Date <= end);

		if (Cursor.TryDecode(cursor, 2) is [var dateText, var lastId])
		{
			var lastDate = DateTime.Parse(dateText, null, System.Globalization.DateTimeStyles.RoundtripKind);
			query = query.Where(invoice => invoice.Date < lastDate || (invoice.Date == lastDate && invoice.Id.CompareTo(lastId) < 0));
		}

		var page = await query.OrderByDescending(invoice => invoice.Date).ThenByDescending(invoice => invoice.Id)
			.Take(pageSize + 1)
			.ToListAsync(cancellationToken);

		var items = page.Take(pageSize).ToList();
		var nextCursor = page.Count > pageSize ? Cursor.Encode(items[^1].Date.ToString("O"), items[^1].Id) : null;
		return new(items, nextCursor);
	}

	/// <exception cref="InvoiceNumberAlreadyExistsException">Number already in use</exception>
	public async Task AddInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
	{
		_db.Invoices.Add(invoice);
		await this.SaveChangesAsync(cancellationToken);
	}

	/// <exception cref="InvoiceNumberAlreadyExistsException">Number already in use</exception>
	public async Task<int> UpdateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
	{
		_db.Invoices.Update(invoice);
		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	public async Task<int> DeleteInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
	{
		_db.Invoices.Remove(invoice);
		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	/// <remarks>
	/// Stock is decremented with a conditional <c>UPDATE ... WHERE Amount &gt;= @amount</c> per line (no oversell under
	/// concurrent sales, the same guarantee the Mongo conditional <c>$inc</c> gave), then the invoice is stored;
	/// all in one Postgres transaction.
	/// </remarks>
	/// <exception cref="InvoiceNumberAlreadyExistsException">Number already exists; nothing written</exception>
	public async Task<IReadOnlyList<string>> TryAddSaleAsync(Invoice invoice, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(invoice);
		var items = invoice.Items ?? [];

		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		List<(ShoppingCartItem Item, int AmountLeft)> taken = [];
		foreach (var item in items)
		{
			var affected = await _db.StockItems
				.Where(stockItem => stockItem.Code == item.StockItem.Code && stockItem.Amount >= item.Amount)
				.ExecuteUpdateAsync(setters => setters.SetProperty(stockItem => stockItem.Amount, stockItem => stockItem.Amount - item.Amount), cancellationToken);

			if (affected == 0)
			{
				await transaction.RollbackAsync(cancellationToken);
				return [item.StockItem.Name];
			}

			// Reuse the tracked instance: a detached one with the same Id would conflict when Invoices.Add() below reaches it via the Items navigation
			var stored = await _db.StockItems.SingleAsync(stockItem => stockItem.Code == item.StockItem.Code, cancellationToken);
			_db.Transactions.Add(new Transaction(stored, DateTime.Now, Transaction.Kind.Amount, -item.Amount));
			item.StockItem = stored;
			taken.Add((item, stored.Amount));
		}

		// Reuse the tracked instance, same reason as the StockItem swap above
		invoice.Customer = await _db.Customers.FindAsync([invoice.Customer.Id], cancellationToken) ?? invoice.Customer;
		_db.Invoices.Add(invoice);
		// Outbox row for the SIFEN transmission worker (ADR-0031); same transaction as the invoice write
		_db.PendingTransmissions.Add(new PendingTransmission(invoice, DateTime.Now));

		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			await transaction.RollbackAsync(cancellationToken);
			throw new InvoiceNumberAlreadyExistsException();
		}

		await transaction.CommitAsync(cancellationToken);

		taken.ForEach(line => line.Item.StockItem.Amount = line.AmountLeft);
		return [];
	}

	public async Task<IReadOnlyList<Invoice>> GetStuckTransmissionsAsync(CancellationToken cancellationToken = default)
	{
		return await _db.Invoices
			.Where(invoice => invoice.TransmissionStatus == TransmissionStatus.Rejected || invoice.TransmissionStatus == TransmissionStatus.Error)
			.OrderByDescending(invoice => invoice.Date)
			.ToListAsync(cancellationToken);
	}

	private async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			throw new InvoiceNumberAlreadyExistsException();
		}
	}
}
