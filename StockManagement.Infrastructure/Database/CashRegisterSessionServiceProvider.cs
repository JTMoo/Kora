using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ICashRegisterSessionServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class CashRegisterSessionServiceProvider(AppDbContext db) : ICashRegisterSessionServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<CashRegisterSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		return _db.CashRegisterSessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);
	}

	public Task<CashRegisterSession?> GetOpenSessionAsync(CancellationToken cancellationToken = default)
	{
		return _db.CashRegisterSessions.SingleOrDefaultAsync(session => session.Status == CashRegisterSessionStatus.Open, cancellationToken);
	}

	/// <summary>Keyset page by <see cref="CashRegisterSession.OpenedAt"/> descending then <see cref="Database.BaseDocument.Id"/> descending (ADR-0029)</summary>
	public async Task<CursorPage<CashRegisterSession>> GetSessionsAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = _db.CashRegisterSessions.AsQueryable();
		if (Cursor.TryDecode(cursor, 2) is [var dateText, var lastId])
		{
			var lastDate = DateTime.Parse(dateText, null, System.Globalization.DateTimeStyles.RoundtripKind);
			query = query.Where(session => session.OpenedAt < lastDate || (session.OpenedAt == lastDate && session.Id.CompareTo(lastId) < 0));
		}

		var page = await query.OrderByDescending(session => session.OpenedAt).ThenByDescending(session => session.Id).Take(pageSize + 1).ToListAsync(cancellationToken);

		var items = page.Take(pageSize).ToList();
		var nextCursor = page.Count > pageSize ? Cursor.Encode(items[^1].OpenedAt.ToString("O"), items[^1].Id) : null;
		return new(items, nextCursor);
	}

	public async Task AddSessionAsync(CashRegisterSession session, CancellationToken cancellationToken = default)
	{
		_db.CashRegisterSessions.Add(session);
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task<int> UpdateSessionAsync(CashRegisterSession session, CancellationToken cancellationToken = default)
	{
		_db.CashRegisterSessions.Update(session);
		return await _db.SaveChangesAsync(cancellationToken);
	}

	/// <remarks>Sales payments only (cash received at the till); supplier payments are cash leaving the business, not part of this reconciliation</remarks>
	public Task<decimal> GetCashPaymentsTotalAsync(string sessionId, CancellationToken cancellationToken = default)
	{
		return _db.Invoices.SelectMany(invoice => invoice.Payments).Where(payment => payment.CashRegisterSessionId == sessionId).SumAsync(payment => payment.Amount, cancellationToken);
	}

	public async Task<Dictionary<string, decimal>> GetCashPaymentsTotalsAsync(IReadOnlyCollection<string> sessionIds, CancellationToken cancellationToken = default)
	{
		return await _db.Invoices.SelectMany(invoice => invoice.Payments)
			.Where(payment => payment.CashRegisterSessionId != null && sessionIds.Contains(payment.CashRegisterSessionId))
			.GroupBy(payment => payment.CashRegisterSessionId!)
			.Select(group => new { SessionId = group.Key, Total = group.Sum(payment => payment.Amount) })
			.ToDictionaryAsync(entry => entry.SessionId, entry => entry.Total, cancellationToken);
	}
}
