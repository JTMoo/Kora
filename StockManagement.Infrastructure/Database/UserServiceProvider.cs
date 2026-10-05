using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IUserServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class UserServiceProvider(AppDbContext db) : IUserServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<User?> GetUserAsync(string id, CancellationToken cancellationToken = default)
	{
		return _db.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
	}

	public Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
	{
		return _db.Users.SingleOrDefaultAsync(user => user.Username == username, cancellationToken);
	}

	public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
	{
		return await _db.Users.ToListAsync(cancellationToken);
	}

	/// <summary>Keyset page by <see cref="User.Username"/> then <see cref="User.Id"/> (ADR-0029)</summary>
	public async Task<CursorPage<User>> GetUsersAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = _db.Users.AsQueryable();
		if (Cursor.TryDecode(cursor, 2) is [var lastUsername, var lastId])
		{
			query = query.Where(user => user.Username.CompareTo(lastUsername) > 0 || (user.Username == lastUsername && user.Id.CompareTo(lastId) > 0));
		}

		var page = await query.OrderBy(user => user.Username).ThenBy(user => user.Id).Take(pageSize + 1).ToListAsync(cancellationToken);

		var items = page.Take(pageSize).ToList();
		var nextCursor = page.Count > pageSize ? Cursor.Encode(items[^1].Username, items[^1].Id) : null;
		return new(items, nextCursor);
	}

	/// <exception cref="UsernameAlreadyExistsException">Username already in use</exception>
	public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
	{
		_db.Users.Add(user);
		await this.SaveChangesAsync(cancellationToken);
	}

	/// <exception cref="UsernameAlreadyExistsException">Username already in use</exception>
	public async Task<int> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
	{
		_db.Users.Update(user);
		await this.SaveChangesAsync(cancellationToken);
		return 1;
	}

	public async Task<int> DeleteUserAsync(User user, CancellationToken cancellationToken = default)
	{
		_db.Users.Remove(user);
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
			throw new UsernameAlreadyExistsException();
		}
	}
}
