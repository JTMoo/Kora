using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ISupplierServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class SupplierServiceProvider(AppDbContext db) : ISupplierServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<Supplier> GetSupplierByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		return _db.Suppliers.SingleOrDefaultAsync(supplier => supplier.Id == id, cancellationToken)!;
	}

	public async Task<IEnumerable<Supplier>> GetAllSuppliersAsync(CancellationToken cancellationToken = default)
	{
		return await _db.Suppliers.ToListAsync(cancellationToken);
	}

	/// <summary>Keyset page by <see cref="Supplier.Name"/> then <see cref="Supplier.Id"/> (ADR-0029)</summary>
	public async Task<CursorPage<Supplier>> GetSuppliersAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = _db.Suppliers.AsQueryable();
		if (Cursor.TryDecode(cursor, 2) is [var lastName, var lastId])
		{
			query = query.Where(supplier => supplier.Name.CompareTo(lastName) > 0 || (supplier.Name == lastName && supplier.Id.CompareTo(lastId) > 0));
		}

		var page = await query.OrderBy(supplier => supplier.Name).ThenBy(supplier => supplier.Id).Take(pageSize + 1).ToListAsync(cancellationToken);

		var items = page.Take(pageSize).ToList();
		var nextCursor = page.Count > pageSize ? Cursor.Encode(items[^1].Name, items[^1].Id) : null;
		return new(items, nextCursor);
	}

	/// <exception cref="SupplierNameAlreadyExistsException">Name already in use</exception>
	public async Task AddSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
	{
		_db.Suppliers.Add(supplier);
		await this.SaveChangesAsync(cancellationToken);
	}

	/// <exception cref="SupplierNameAlreadyExistsException">Name already in use</exception>
	public async Task<int> UpdateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
	{
		_db.Suppliers.Update(supplier);
		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	/// <exception cref="SupplierInUseException">Assigned to at least one stock item</exception>
	public async Task<int> DeleteSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
	{
		_db.Suppliers.Remove(supplier);
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
			throw new SupplierNameAlreadyExistsException();
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23503" })
		{
			throw new SupplierInUseException();
		}
	}
}
