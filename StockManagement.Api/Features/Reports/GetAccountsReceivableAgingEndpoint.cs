using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.Reports;


public sealed record GetAccountsReceivableAgingRequest(DateTime? AsOf, string? Cursor, int? PageSize);


/// <param name="NextCursor">Opaque cursor for the next page of <see cref="Items"/>; <see langword="null"/> on the last page</param>
public sealed record AccountsReceivableAgingResponse(AccountsReceivableAgingTotalsResponse Totals, IReadOnlyList<AccountsReceivableAgingRowResponse> Items, string? NextCursor);


public sealed record AccountsReceivableAgingTotalsResponse(decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Days90Plus, decimal Total);


public sealed record AccountsReceivableAgingRowResponse(int CustomerId, string CustomerName, decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Days90Plus, decimal Total);


/// <remarks>Open invoices (#55/#163) bucketed by days overdue as of <c>AsOf</c> (default now), excluding cancelled invoices</remarks>
public class GetAccountsReceivableAgingEndpoint(IReportServiceProvider reportServiceProvider) : Endpoint<GetAccountsReceivableAgingRequest, AccountsReceivableAgingResponse>
{
	private readonly IReportServiceProvider _reportServiceProvider = reportServiceProvider;


	public override void Configure()
	{
		this.Get("/reports/accounts-receivable-aging");
		this.Permissions(Permission.ReportsRead);
	}

	public override async Task<AccountsReceivableAgingResponse> ExecuteAsync(GetAccountsReceivableAgingRequest request, CancellationToken cancellationToken)
	{
		var asOf = request.AsOf ?? DateTime.Now;
		var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);

		var totals = await _reportServiceProvider.GetAccountsReceivableAgingTotalsAsync(asOf, cancellationToken);
		var page = await _reportServiceProvider.GetAccountsReceivableAgingByCustomerAsync(asOf, request.Cursor, pageSize, cancellationToken);

		var totalsResponse = new AccountsReceivableAgingTotalsResponse(totals.Current, totals.Days1To30, totals.Days31To60, totals.Days61To90, totals.Days90Plus, totals.Total);
		var items = page.Items.Select(row => new AccountsReceivableAgingRowResponse(row.CustomerId, row.CustomerName, row.Current, row.Days1To30, row.Days31To60, row.Days61To90, row.Days90Plus, row.Total)).ToList();
		return new(totalsResponse, items, page.NextCursor);
	}
}
