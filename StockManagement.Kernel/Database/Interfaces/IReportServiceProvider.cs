namespace StockManagement.Kernel.Database.Interfaces;


/// <summary>
/// Read-only aggregates for the Reports screens (#163): current stock valuation and sales totals
/// </summary>
public interface IReportServiceProvider
{
	/// <returns>Sum of <c>Amount * Price</c> and sum of <c>Amount</c> across every stock item</returns>
	Task<StockValueTotals> GetStockValueTotalsAsync(CancellationToken cancellationToken = default);

	/// <summary>Invoices dated in [<paramref name="from"/>, <paramref name="to"/>], excluding cancelled, grouped by day, earliest first</summary>
	Task<IReadOnlyList<SalesByPeriodRow>> GetSalesByPeriodAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

	/// <summary>Same invoices grouped by customer, highest total first, one page at a time</summary>
	Task<CursorPage<SalesByCustomerRow>> GetSalesByCustomerAsync(DateTime from, DateTime to, string? cursor, int pageSize, CancellationToken cancellationToken = default);
}


public sealed record StockValueTotals(decimal TotalValue, int TotalUnits);


public sealed record SalesByPeriodRow(DateTime Date, int InvoiceCount, decimal Total, decimal Tax);


public sealed record SalesByCustomerRow(int CustomerId, string CustomerName, int InvoiceCount, decimal Total);
