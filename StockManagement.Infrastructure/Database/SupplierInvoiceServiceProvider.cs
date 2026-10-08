using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ISupplierInvoiceServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class SupplierInvoiceServiceProvider(AppDbContext db) : ISupplierInvoiceServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<SupplierInvoice> GetSupplierInvoiceAsync(string number, CancellationToken cancellationToken = default)
	{
		return _db.SupplierInvoices.SingleOrDefaultAsync(invoice => invoice.Number == number, cancellationToken)!;
	}

	public async Task<IEnumerable<SupplierInvoice>> GetSupplierInvoicesAsync(CancellationToken cancellationToken = default)
	{
		return await _db.SupplierInvoices.ToListAsync(cancellationToken);
	}

	/// <summary>Keyset page by <see cref="SupplierInvoice.Date"/> descending then <see cref="BaseDocument.Id"/> (ADR-0029)</summary>
	public async Task<CursorPage<SupplierInvoice>> GetSupplierInvoicesAsync(string? supplierId, string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = _db.SupplierInvoices.AsQueryable();
		if (supplierId is string id) query = query.Where(invoice => invoice.Supplier.Id == id);

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

	/// <exception cref="SupplierInvoiceNumberAlreadyExistsException">Number already in use</exception>
	/// <remarks>Checks in every <see cref="SupplierInvoiceItem.Amount"/> as stock, atomically with the invoice insert (#246)</remarks>
	public async Task AddSupplierInvoiceAsync(SupplierInvoice invoice, CancellationToken cancellationToken = default)
	{
		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		_db.SupplierInvoices.Add(invoice);

		foreach (var item in invoice.Items)
		{
			// Track the StockItem's own state first: Add() on the Transaction below would otherwise graph-fixup it as Added too
			_db.StockItems.Attach(item.StockItem);
			await _db.StockItems
				.Where(stockItem => stockItem.Id == item.StockItem.Id)
				.ExecuteUpdateAsync(setters => setters.SetProperty(stockItem => stockItem.Amount, stockItem => stockItem.Amount + item.Amount), cancellationToken);

			_db.Transactions.Add(new Transaction(item.StockItem, DateTime.Now, Transaction.Kind.Amount, item.Amount, $"Goods receipt - supplier invoice {invoice.Number}"));
		}

		try
		{
			await this.SaveChangesAsync(cancellationToken);
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}

		await transaction.CommitAsync(cancellationToken);
		foreach (var item in invoice.Items) item.StockItem.Amount += item.Amount;
	}

	public async Task<int> UpdateSupplierInvoiceAsync(SupplierInvoice invoice, CancellationToken cancellationToken = default)
	{
		_db.SupplierInvoices.Update(invoice);
		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	private async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			throw new SupplierInvoiceNumberAlreadyExistsException();
		}
	}
}
