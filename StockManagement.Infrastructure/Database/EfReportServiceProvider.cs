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

	public async Task<AccountsReceivableAgingTotals> GetAccountsReceivableAgingTotalsAsync(DateTime asOf, CancellationToken cancellationToken = default)
	{
		var asOfDate = asOf.Date;

		// Each bucket summed in its own call: EF won't translate Sum() reached through a Select() projecting into another type's member (same reason the per-customer query below groups into an anonymous type first)
		var query = this.OpenInvoiceAgingQuery(asOfDate);
		return new(
			await query.SumAsync(invoice => invoice.Current, cancellationToken),
			await query.SumAsync(invoice => invoice.Days1To30, cancellationToken),
			await query.SumAsync(invoice => invoice.Days31To60, cancellationToken),
			await query.SumAsync(invoice => invoice.Days61To90, cancellationToken),
			await query.SumAsync(invoice => invoice.Days90Plus, cancellationToken),
			await query.SumAsync(invoice => invoice.AmountDue, cancellationToken));
	}

	/// <remarks>Keyset page by amount due descending then <see cref="SalesByCustomerRow.CustomerId"/> (ADR-0029), same shape as <see cref="GetSalesByCustomerAsync"/></remarks>
	public async Task<CursorPage<AccountsReceivableAgingRow>> GetAccountsReceivableAgingByCustomerAsync(DateTime asOf, string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = this.OpenInvoiceAgingQuery(asOf.Date)
			.GroupBy(invoice => new { invoice.CustomerId, invoice.CustomerName })
			.Select(group => new
			{
				group.Key.CustomerId,
				group.Key.CustomerName,
				Current = group.Sum(invoice => invoice.Current),
				Days1To30 = group.Sum(invoice => invoice.Days1To30),
				Days31To60 = group.Sum(invoice => invoice.Days31To60),
				Days61To90 = group.Sum(invoice => invoice.Days61To90),
				Days90Plus = group.Sum(invoice => invoice.Days90Plus),
				Total = group.Sum(invoice => invoice.AmountDue)
			});

		if (Cursor.TryDecode(cursor, 2) is [var lastTotalText, var lastCustomerIdText]
			&& decimal.TryParse(lastTotalText, NumberStyles.Number, CultureInfo.InvariantCulture, out var lastTotal)
			&& int.TryParse(lastCustomerIdText, out var lastCustomerId))
		{
			query = query.Where(row => row.Total < lastTotal || (row.Total == lastTotal && row.CustomerId > lastCustomerId));
		}

		var page = await query.OrderByDescending(row => row.Total).ThenBy(row => row.CustomerId).Take(pageSize + 1).ToListAsync(cancellationToken);

		var items = page.Take(pageSize)
			.Select(row => new AccountsReceivableAgingRow(row.CustomerId, row.CustomerName, row.Current, row.Days1To30, row.Days31To60, row.Days61To90, row.Days90Plus, row.Total))
			.ToList();
		var nextCursor = page.Count > pageSize
			? Cursor.Encode(items[^1].Total.ToString(CultureInfo.InvariantCulture), items[^1].CustomerId.ToString(CultureInfo.InvariantCulture))
			: null;
		return new(items, nextCursor);
	}

	/// <summary>Per-invoice amount due (zero once cancelled, same rule as <c>InvoiceStatusCalculator</c>) split into the bucket matching its days overdue vs. <paramref name="asOfDate"/>, computed and filtered server-side; invoices fully paid or with no amount due are left out</summary>
	private IQueryable<AccountsReceivableAgingBucket> OpenInvoiceAgingQuery(DateTime asOfDate)
	{
		return _db.Invoices
			.Where(invoice => !invoice.IsCancelled)
			.Select(invoice => new
			{
				invoice.Customer.CustomerId,
				Name = invoice.Customer.Name + " " + invoice.Customer.Lastname,
				invoice.ExpirationDate,
				AmountDue = invoice.Total - invoice.Payments.Sum(payment => payment.Amount)
			})
			.Where(invoice => invoice.AmountDue > 0)
			.Select(invoice => new AccountsReceivableAgingBucket
			{
				CustomerId = invoice.CustomerId,
				CustomerName = invoice.Name,
				Current = invoice.ExpirationDate.Date >= asOfDate ? invoice.AmountDue : 0,
				Days1To30 = invoice.ExpirationDate.Date < asOfDate && invoice.ExpirationDate.Date >= asOfDate.AddDays(-30) ? invoice.AmountDue : 0,
				Days31To60 = invoice.ExpirationDate.Date < asOfDate.AddDays(-30) && invoice.ExpirationDate.Date >= asOfDate.AddDays(-60) ? invoice.AmountDue : 0,
				Days61To90 = invoice.ExpirationDate.Date < asOfDate.AddDays(-60) && invoice.ExpirationDate.Date >= asOfDate.AddDays(-90) ? invoice.AmountDue : 0,
				Days90Plus = invoice.ExpirationDate.Date < asOfDate.AddDays(-90) ? invoice.AmountDue : 0,
				AmountDue = invoice.AmountDue
			});
	}


	/// <summary>Plain class, not a record: EF maps a <c>Sum(x => x.Current)</c> reached through a prior <c>Select</c> back to that Select's constructor arguments only via an object-initializer's member assignments, not a positional record's constructor</summary>
	private sealed class AccountsReceivableAgingBucket
	{
		public int CustomerId { get; set; }
		public string CustomerName { get; set; } = "";
		public decimal Current { get; set; }
		public decimal Days1To30 { get; set; }
		public decimal Days31To60 { get; set; }
		public decimal Days61To90 { get; set; }
		public decimal Days90Plus { get; set; }
		public decimal AmountDue { get; set; }
	}
}
