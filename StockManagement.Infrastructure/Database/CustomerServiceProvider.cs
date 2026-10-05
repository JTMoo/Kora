using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ICustomerServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class CustomerServiceProvider(AppDbContext db) : ICustomerServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<Customer> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default)
	{
		return _db.Customers.SingleOrDefaultAsync(customer => customer.CustomerId == customerId, cancellationToken)!;
	}

	public Task<Customer> GetCustomerByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		return _db.Customers.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken)!;
	}

	public async Task<IEnumerable<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
	{
		return await _db.Customers.ToListAsync(cancellationToken);
	}

	/// <summary>Keyset page by <see cref="Customer.Name"/> then <see cref="Customer.Id"/> (ADR-0029)</summary>
	public async Task<CursorPage<Customer>> GetCustomersAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = _db.Customers.AsQueryable();
		if (Cursor.TryDecode(cursor, 2) is [var lastName, var lastId])
		{
			query = query.Where(customer => customer.Name.CompareTo(lastName) > 0 || (customer.Name == lastName && customer.Id.CompareTo(lastId) > 0));
		}

		var page = await query.OrderBy(customer => customer.Name).ThenBy(customer => customer.Id).Take(pageSize + 1).ToListAsync(cancellationToken);

		var items = page.Take(pageSize).ToList();
		var nextCursor = page.Count > pageSize ? Cursor.Encode(items[^1].Name, items[^1].Id) : null;
		return new(items, nextCursor);
	}

	/// <exception cref="CustomerIdAlreadyExistsException">Customer id already in use</exception>
	/// <exception cref="CustomerIdentificationNumberAlreadyExistsException">Identification number already in use</exception>
	public async Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
	{
		_db.Customers.Add(customer);
		await this.SaveChangesAsync(cancellationToken);
	}

	/// <exception cref="CustomerIdAlreadyExistsException">Customer id already in use</exception>
	/// <exception cref="CustomerIdentificationNumberAlreadyExistsException">Identification number already in use</exception>
	public async Task AddManyCustomersAsync(IList<Customer> customers, CancellationToken cancellationToken = default)
	{
		if (customers is not { Count: > 0 }) return;

		_db.Customers.AddRange(customers);
		await this.SaveChangesAsync(cancellationToken);
	}

	/// <exception cref="CustomerIdAlreadyExistsException">Customer id already in use</exception>
	/// <exception cref="CustomerIdentificationNumberAlreadyExistsException">Identification number already in use</exception>
	public async Task<int> UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
	{
		_db.Customers.Update(customer);
		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	public async Task<int> DeleteCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
	{
		_db.Customers.Remove(customer);
		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	private async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505", ConstraintName: CustomerConfiguration.IdentificationNumberIndexName })
		{
			throw new CustomerIdentificationNumberAlreadyExistsException();
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			throw new CustomerIdAlreadyExistsException();
		}
	}
}
