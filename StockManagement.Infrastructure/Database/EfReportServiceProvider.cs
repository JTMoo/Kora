using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IReportServiceProvider"/> via EF aggregate queries on <see cref="AppDbContext"/>
/// </summary>
public class EfReportServiceProvider(AppDbContext db) : IReportServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task<StockValueTotals> GetStockValueTotalsAsync(CancellationToken cancellationToken = default)
	{
		var totalUnits = await _db.StockItems.SumAsync(item => item.Amount, cancellationToken);
		var totalValue = await _db.StockItems.SumAsync(item => item.Amount * item.Price, cancellationToken);
		return new(totalValue, totalUnits);
	}

	public async Task<IReadOnlyList<SalesByPeriodRow>> GetSalesByPeriodAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
	{
		// Grouped into an anonymous type first: EF can't translate a GroupBy().Select() straight into a record constructor call
		var rows = await _db.Invoices
			.Where(invoice => invoice.Date >= from && invoice.Date <= to && !invoice.IsCancelled)
			.GroupBy(invoice => invoice.Date.Date)
			.Select(group => new { Date = group.Key, InvoiceCount = group.Count(), Total = group.Sum(invoice => invoice.Total), Tax = group.Sum(invoice => invoice.Tax) })
			.OrderBy(row => row.Date)
			.ToListAsync(cancellationToken);

		return rows.Select(row => new SalesByPeriodRow(row.Date, row.InvoiceCount, row.Total, row.Tax)).ToList();
	}

	/// <summary>Keyset page by <see cref="SalesByCustomerRow.Total"/> descending then <see cref="SalesByCustomerRow.CustomerId"/> (ADR-0029)</summary>
	public async Task<CursorPage<SalesByCustomerRow>> GetSalesByCustomerAsync(DateTime from, DateTime to, string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		// Grouped into an anonymous type first, same reason as GetSalesByPeriodAsync
		var query = _db.Invoices
			.Where(invoice => invoice.Date >= from && invoice.Date <= to && !invoice.IsCancelled)
			.GroupBy(invoice => new { invoice.Customer.CustomerId, Name = invoice.Customer.Name + " " + invoice.Customer.Lastname })
			.Select(group => new { group.Key.CustomerId, group.Key.Name, InvoiceCount = group.Count(), Total = group.Sum(invoice => invoice.Total) });

		if (Cursor.TryDecode(cursor, 2) is [var lastTotalText, var lastCustomerIdText]
			&& decimal.TryParse(lastTotalText, NumberStyles.Number, CultureInfo.InvariantCulture, out var lastTotal)
			&& int.TryParse(lastCustomerIdText, out var lastCustomerId))
		{
			query = query.Where(row => row.Total < lastTotal || (row.Total == lastTotal && row.CustomerId > lastCustomerId));
		}

		var page = await query.OrderByDescending(row => row.Total).ThenBy(row => row.CustomerId).Take(pageSize + 1).ToListAsync(cancellationToken);

		var items = page.Take(pageSize).Select(row => new SalesByCustomerRow(row.CustomerId, row.Name, row.InvoiceCount, row.Total)).ToList();
		var nextCursor = page.Count > pageSize
			? Cursor.Encode(items[^1].Total.ToString(CultureInfo.InvariantCulture), items[^1].CustomerId.ToString(CultureInfo.InvariantCulture))
			: null;
		return new(items, nextCursor);
	}
}
