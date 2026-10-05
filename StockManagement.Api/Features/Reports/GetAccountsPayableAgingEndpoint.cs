using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.Reports;


public sealed record GetAccountsPayableAgingRequest(DateTime? AsOf, string? Cursor, int? PageSize);


/// <param name="NextCursor">Opaque cursor for the next page of <see cref="Items"/>; <see langword="null"/> on the last page</param>
public sealed record AccountsPayableAgingResponse(AccountsPayableAgingTotalsResponse Totals, IReadOnlyList<AccountsPayableAgingRowResponse> Items, string? NextCursor);


public sealed record AccountsPayableAgingTotalsResponse(decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Days90Plus, decimal Total);


public sealed record AccountsPayableAgingRowResponse(string SupplierId, string SupplierName, decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Days90Plus, decimal Total);


/// <remarks>Open supplier invoices (#207) bucketed by days overdue as of <c>AsOf</c> (default now)</remarks>
public class GetAccountsPayableAgingEndpoint(IReportServiceProvider reportServiceProvider) : Endpoint<GetAccountsPayableAgingRequest, AccountsPayableAgingResponse>
{
	private readonly IReportServiceProvider _reportServiceProvider = reportServiceProvider;


	public override void Configure()
	{
		this.Get("/reports/accounts-payable-aging");
		this.Permissions(Permission.ReportsRead);
	}

	public override async Task<AccountsPayableAgingResponse> ExecuteAsync(GetAccountsPayableAgingRequest request, CancellationToken cancellationToken)
	{
		var asOf = request.AsOf ?? DateTime.Now;
		var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);

		var totals = await _reportServiceProvider.GetAccountsPayableAgingTotalsAsync(asOf, cancellationToken);
		var page = await _reportServiceProvider.GetAccountsPayableAgingBySupplierAsync(asOf, request.Cursor, pageSize, cancellationToken);

		var totalsResponse = new AccountsPayableAgingTotalsResponse(totals.Current, totals.Days1To30, totals.Days31To60, totals.Days61To90, totals.Days90Plus, totals.Total);
		var items = page.Items.Select(row => new AccountsPayableAgingRowResponse(row.SupplierId, row.SupplierName, row.Current, row.Days1To30, row.Days31To60, row.Days61To90, row.Days90Plus, row.Total)).ToList();
		return new(totalsResponse, items, page.NextCursor);
	}
}
