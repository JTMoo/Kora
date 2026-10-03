using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IStockItemServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
/// <remarks>Every write records a <see cref="Transaction"/> in the same <c>SaveChangesAsync</c> call</remarks>
public class EfStockItemServiceProvider(AppDbContext db) : IStockItemServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<StockItem> GetStockItemAsync(string code, CancellationToken cancellationToken = default)
	{
		return _db.StockItems.Include(item => item.Supplier).SingleOrDefaultAsync(item => item.Code == code, cancellationToken)!;
	}

	public Task<StockItem> GetStockItemByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		return _db.StockItems.Include(item => item.Supplier).SingleOrDefaultAsync(item => item.Id == id, cancellationToken)!;
	}

	public Task<StockItem?> GetStockItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(barcode)) return Task.FromResult<StockItem?>(null);
		return _db.StockItems.Include(item => item.Supplier).SingleOrDefaultAsync(item => item.Barcode == barcode, cancellationToken);
	}

	public async Task<IEnumerable<StockItem>> GetAllStockItemsAsync(CancellationToken cancellationToken = default)
	{
		return await _db.StockItems.Include(item => item.Supplier).ToListAsync(cancellationToken);
	}

	public Task<CursorPage<StockItem>> GetStockItemsAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		return this.GetPageAsync(_db.StockItems.Include(item => item.Supplier), cursor, pageSize, cancellationToken);
	}

	public async Task<IEnumerable<StockItem>> GetStockItemsBelowMinimumAsync(CancellationToken cancellationToken = default)
	{
		return await _db.StockItems.Include(item => item.Supplier)
			.Where(item => item.MinimumStock > 0 && item.Amount < item.MinimumStock)
			.ToListAsync(cancellationToken);
	}

	public Task<CursorPage<StockItem>> GetStockItemsBelowMinimumAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = _db.StockItems.Include(item => item.Supplier).Where(item => item.MinimumStock > 0 && item.Amount < item.MinimumStock);
		return this.GetPageAsync(query, cursor, pageSize, cancellationToken);
	}

	/// <summary>Keyset page by <see cref="StockItem.Code"/> then <see cref="StockItem.Id"/> (ADR-0029)</summary>
	private async Task<CursorPage<StockItem>> GetPageAsync(IQueryable<StockItem> query, string? cursor, int pageSize, CancellationToken cancellationToken)
	{
		if (Cursor.TryDecode(cursor, 2) is [var lastCode, var lastId])
		{
			query = query.Where(item => item.Code.CompareTo(lastCode) > 0 || (item.Code == lastCode && item.Id.CompareTo(lastId) > 0));
		}

		var page = await query.OrderBy(item => item.Code).ThenBy(item => item.Id).Take(pageSize + 1).ToListAsync(cancellationToken);

		var items = page.Take(pageSize).ToList();
		var nextCursor = page.Count > pageSize ? Cursor.Encode(items[^1].Code, items[^1].Id) : null;
		return new(items, nextCursor);
	}

	/// <exception cref="StockItemCodeAlreadyExistsException">Code already in use</exception>
	/// <exception cref="StockItemBarcodeAlreadyExistsException">Barcode already in use</exception>
	public async Task AddStockItemAsync(StockItem stockItem, CancellationToken cancellationToken = default)
	{
		_db.StockItems.Add(stockItem);
		_db.Transactions.Add(new Transaction(stockItem, DateTime.Now, Transaction.Kind.Amount, stockItem.Amount));
		await this.SaveChangesAsync(cancellationToken);
	}

	/// <exception cref="StockItemCodeAlreadyExistsException">Code already in use</exception>
	public async Task AddManyStockItemsAsync(IList<StockItem> stockItem, CancellationToken cancellationToken = default)
	{
		_db.StockItems.AddRange(stockItem);
		await this.SaveChangesAsync(cancellationToken);
	}

	/// <exception cref="StockItemCodeAlreadyExistsException">Code already in use</exception>
	/// <exception cref="StockItemBarcodeAlreadyExistsException">Barcode already in use</exception>
	public async Task<int> UpdateStockItemAsync(StockItem stockItem, CancellationToken cancellationToken = default)
	{
		// Track StockItem's own state first: Add() on the Transaction below would otherwise graph-fixup StockItem as Added too
		var stored = await _db.StockItems.AsNoTracking().SingleOrDefaultAsync(item => item.Id == stockItem.Id, cancellationToken);
		_db.StockItems.Update(stockItem);

		if (stored is not null && stored.Amount != stockItem.Amount)
		{
			_db.Transactions.Add(new Transaction(stockItem, DateTime.Now, Transaction.Kind.Amount, stockItem.Amount - stored.Amount));
		}

		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	public async Task<int> DeleteStockItemAsync(StockItem stockItem, CancellationToken cancellationToken = default)
	{
		// Same ordering reason as UpdateStockItemAsync
		_db.StockItems.Remove(stockItem);
		_db.Transactions.Add(new Transaction(stockItem, DateTime.Now, Transaction.Kind.Deletion, 1));
		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	public async Task CheckInStockItemAsync(StockItem stockItem, int amount, string reason, CancellationToken cancellationToken = default)
	{
		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		// Track StockItem's own state first: Add() on the Transaction below would otherwise graph-fixup StockItem as Added too
		this.AttachUnchanged(stockItem);
		await _db.StockItems
			.Where(item => item.Id == stockItem.Id)
			.ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Amount, item => item.Amount + amount), cancellationToken);

		_db.Transactions.Add(new Transaction(stockItem, DateTime.Now, Transaction.Kind.Amount, amount, reason));
		await this.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);
		stockItem.Amount += amount;
	}

	/// <remarks>Conditional <c>UPDATE ... WHERE Amount &gt;= @amount</c>, same oversell guard as <see cref="EfInvoiceServiceProvider.TryAddSaleAsync"/></remarks>
	public async Task<bool> TryCheckOutStockItemAsync(StockItem stockItem, int amount, string reason, CancellationToken cancellationToken = default)
	{
		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		// Same ordering reason as CheckInStockItemAsync
		this.AttachUnchanged(stockItem);
		var affected = await _db.StockItems
			.Where(item => item.Id == stockItem.Id && item.Amount >= amount)
			.ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Amount, item => item.Amount - amount), cancellationToken);

		if (affected == 0)
		{
			await transaction.RollbackAsync(cancellationToken);
			return false;
		}

		_db.Transactions.Add(new Transaction(stockItem, DateTime.Now, Transaction.Kind.Amount, -amount, reason));
		await this.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);
		stockItem.Amount -= amount;
		return true;
	}

	/// <summary>
	/// Registers an already-stored, unmodified <see cref="StockItem"/> with the change tracker; a no-op if this exact instance is tracked already
	/// </summary>
	private void AttachUnchanged(StockItem stockItem)
	{
		_db.StockItems.Attach(stockItem);
	}

	/// <summary>
	/// Every write succeeds fully or throws: EF has no partial-row upsert like the old Mongo layer
	/// </summary>
	private async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505", ConstraintName: StockItemConfiguration.BarcodeIndexName })
		{
			throw new StockItemBarcodeAlreadyExistsException();
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			throw new StockItemCodeAlreadyExistsException();
		}
	}
}
