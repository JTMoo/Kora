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
		var buckets = await this.LoadOpenInvoiceAgingAsync(asOf, cancellationToken);
		return new(
			buckets.Sum(bucket => bucket.Current),
			buckets.Sum(bucket => bucket.Days1To30),
			buckets.Sum(bucket => bucket.Days31To60),
			buckets.Sum(bucket => bucket.Days61To90),
			buckets.Sum(bucket => bucket.Days90Plus),
			buckets.Sum(bucket => bucket.AmountDue));
	}

	/// <remarks>Keyset page by amount due descending then <see cref="SalesByCustomerRow.CustomerId"/> (ADR-0029), same shape as <see cref="GetSalesByCustomerAsync"/></remarks>
	public async Task<CursorPage<AccountsReceivableAgingRow>> GetAccountsReceivableAgingByCustomerAsync(DateTime asOf, string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var grouped = (await this.LoadOpenInvoiceAgingAsync(asOf, cancellationToken))
			.GroupBy(bucket => new { bucket.CustomerId, bucket.CustomerName })
			.Select(group => new AccountsReceivableAgingRow(
				group.Key.CustomerId,
				group.Key.CustomerName,
				group.Sum(bucket => bucket.Current),
				group.Sum(bucket => bucket.Days1To30),
				group.Sum(bucket => bucket.Days31To60),
				group.Sum(bucket => bucket.Days61To90),
				group.Sum(bucket => bucket.Days90Plus),
				group.Sum(bucket => bucket.AmountDue)))
			.OrderByDescending(row => row.Total).ThenBy(row => row.CustomerId)
			.ToList();

		var startIndex = 0;
		if (Cursor.TryDecode(cursor, 2) is [var lastTotalText, var lastCustomerIdText]
			&& decimal.TryParse(lastTotalText, NumberStyles.Number, CultureInfo.InvariantCulture, out var lastTotal)
			&& int.TryParse(lastCustomerIdText, out var lastCustomerId))
		{
			startIndex = grouped.FindIndex(row => row.Total < lastTotal || (row.Total == lastTotal && row.CustomerId > lastCustomerId));
			if (startIndex < 0) startIndex = grouped.Count;
		}

		var items = grouped.Skip(startIndex).Take(pageSize).ToList();
		var nextCursor = startIndex + items.Count < grouped.Count
			? Cursor.Encode(items[^1].Total.ToString(CultureInfo.InvariantCulture), items[^1].CustomerId.ToString(CultureInfo.InvariantCulture))
			: null;
		return new(items, nextCursor);
	}

	/// <summary>Per-invoice amount due (zero once cancelled, same rule as <c>InvoiceStatusCalculator</c>) split into the bucket matching its days overdue vs. <paramref name="asOf"/>; invoices fully paid or with no amount due are left out</summary>
	private async Task<List<InvoiceAgingBucket>> LoadOpenInvoiceAgingAsync(DateTime asOf, CancellationToken cancellationToken)
	{
		var invoices = await _db.Invoices
			.Where(invoice => !invoice.IsCancelled)
			.Select(invoice => new
			{
				invoice.Customer.CustomerId,
				Name = invoice.Customer.Name + " " + invoice.Customer.Lastname,
				invoice.ExpirationDate,
				AmountDue = invoice.Total - invoice.Payments.Sum(payment => payment.Amount)
			})
			.ToListAsync(cancellationToken);

		return invoices
			.Where(invoice => invoice.AmountDue > 0)
			.Select(invoice =>
			{
				var daysOverdue = (asOf.Date - invoice.ExpirationDate.Date).Days;
				return new InvoiceAgingBucket(
					invoice.CustomerId,
					invoice.Name,
					Current: daysOverdue <= 0 ? invoice.AmountDue : 0,
					Days1To30: daysOverdue is >= 1 and <= 30 ? invoice.AmountDue : 0,
					Days31To60: daysOverdue is >= 31 and <= 60 ? invoice.AmountDue : 0,
					Days61To90: daysOverdue is >= 61 and <= 90 ? invoice.AmountDue : 0,
					Days90Plus: daysOverdue > 90 ? invoice.AmountDue : 0,
					AmountDue: invoice.AmountDue);
			})
			.ToList();
	}


	private sealed record InvoiceAgingBucket(int CustomerId, string CustomerName, decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Days90Plus, decimal AmountDue);


	public async Task<AccountsPayableAgingTotals> GetAccountsPayableAgingTotalsAsync(DateTime asOf, CancellationToken cancellationToken = default)
	{
		var buckets = await this.LoadOpenSupplierInvoiceAgingAsync(asOf, cancellationToken);
		return new(
			buckets.Sum(bucket => bucket.Current),
			buckets.Sum(bucket => bucket.Days1To30),
			buckets.Sum(bucket => bucket.Days31To60),
			buckets.Sum(bucket => bucket.Days61To90),
			buckets.Sum(bucket => bucket.Days90Plus),
			buckets.Sum(bucket => bucket.AmountDue));
	}

	/// <remarks>Keyset page by amount due descending then <see cref="SupplierAgingBucket.SupplierId"/> (ADR-0029), same shape as <see cref="GetAccountsReceivableAgingByCustomerAsync"/></remarks>
	public async Task<CursorPage<AccountsPayableAgingRow>> GetAccountsPayableAgingBySupplierAsync(DateTime asOf, string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var grouped = (await this.LoadOpenSupplierInvoiceAgingAsync(asOf, cancellationToken))
			.GroupBy(bucket => new { bucket.SupplierId, bucket.SupplierName })
			.Select(group => new AccountsPayableAgingRow(
				group.Key.SupplierId,
				group.Key.SupplierName,
				group.Sum(bucket => bucket.Current),
				group.Sum(bucket => bucket.Days1To30),
				group.Sum(bucket => bucket.Days31To60),
				group.Sum(bucket => bucket.Days61To90),
				group.Sum(bucket => bucket.Days90Plus),
				group.Sum(bucket => bucket.AmountDue)))
			.OrderByDescending(row => row.Total).ThenBy(row => row.SupplierId)
			.ToList();

		var startIndex = 0;
		if (Cursor.TryDecode(cursor, 2) is [var lastTotalText, var lastSupplierId]
			&& decimal.TryParse(lastTotalText, NumberStyles.Number, CultureInfo.InvariantCulture, out var lastTotal))
		{
			startIndex = grouped.FindIndex(row => row.Total < lastTotal || (row.Total == lastTotal && string.CompareOrdinal(row.SupplierId, lastSupplierId) > 0));
			if (startIndex < 0) startIndex = grouped.Count;
		}

		var items = grouped.Skip(startIndex).Take(pageSize).ToList();
		var nextCursor = startIndex + items.Count < grouped.Count
			? Cursor.Encode(items[^1].Total.ToString(CultureInfo.InvariantCulture), items[^1].SupplierId)
			: null;
		return new(items, nextCursor);
	}

	/// <summary>Per-supplier-invoice amount due split into the bucket matching its days overdue vs. <paramref name="asOf"/>; invoices fully paid are left out</summary>
	private async Task<List<SupplierAgingBucket>> LoadOpenSupplierInvoiceAgingAsync(DateTime asOf, CancellationToken cancellationToken)
	{
		var invoices = await _db.SupplierInvoices
			.Select(invoice => new
			{
				invoice.Supplier.Id,
				invoice.Supplier.Name,
				invoice.ExpirationDate,
				AmountDue = invoice.Total - invoice.Payments.Sum(payment => payment.Amount)
			})
			.ToListAsync(cancellationToken);

		return invoices
			.Where(invoice => invoice.AmountDue > 0)
			.Select(invoice =>
			{
				var daysOverdue = (asOf.Date - invoice.ExpirationDate.Date).Days;
				return new SupplierAgingBucket(
					invoice.Id,
					invoice.Name,
					Current: daysOverdue <= 0 ? invoice.AmountDue : 0,
					Days1To30: daysOverdue is >= 1 and <= 30 ? invoice.AmountDue : 0,
					Days31To60: daysOverdue is >= 31 and <= 60 ? invoice.AmountDue : 0,
					Days61To90: daysOverdue is >= 61 and <= 90 ? invoice.AmountDue : 0,
					Days90Plus: daysOverdue > 90 ? invoice.AmountDue : 0,
					AmountDue: invoice.AmountDue);
			})
			.ToList();
	}


	private sealed record SupplierAgingBucket(string SupplierId, string SupplierName, decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Days90Plus, decimal AmountDue);
}
